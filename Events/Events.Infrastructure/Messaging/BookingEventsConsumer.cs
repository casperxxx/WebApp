using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Events.Application.Caching;
using Events.Application.Messaging;
using Events.Application.Repositories;
using Events.Domain.Entities;
using Shared.Contracts;

namespace Events.Infrastructure.Messaging;

/// <summary>
/// Единый consumer топика booking-events (Confirmed / Cancelled) с порядком по EventId
/// </summary>
internal sealed class BookingEventsConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly IBookingEventPublisher _publisher;
    private readonly ILogger<BookingEventsConsumer> _logger;

    public BookingEventsConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        IBookingEventPublisher publisher,
        ILogger<BookingEventsConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _publisher = publisher;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);

    private void ConsumeLoop(CancellationToken stoppingToken)
    {
        var bootstrapServers = _configuration["Kafka:BootstrapServers"];
        var groupId = _configuration["Kafka:ConsumerGroup"] ?? "events-service";

        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            _logger.LogWarning("Kafka:BootstrapServers не задан — consumer не запускаем");
            return;
        }

        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        }).Build();

        consumer.Subscribe(KafkaTopics.BookingEvents);
        _logger.LogInformation("Подписка на {Topic}, group={Group}", KafkaTopics.BookingEvents, groupId);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result = null;
                try
                {
                    result = consumer.Consume(stoppingToken);
                    if (result?.Message?.Value is null)
                    {
                        continue;
                    }

                    ProcessMessageAsync(result.Message.Value, stoppingToken).GetAwaiter().GetResult();
                    consumer.Commit(result);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Ошибка чтения из Kafka");
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Битый JSON — коммитим offset и пропускаем");
                    if (result is not null)
                    {
                        consumer.Commit(result);
                    }
                }
                catch (Exception ex) when (result is not null)
                {
                    // БД недоступна и т.п. — не коммитим вперёд, возвращаемся к тому же offset
                    _logger.LogError(ex, "Ошибка обработки — Seek на текущее смещение и повтор");
                    consumer.Seek(result.TopicPartitionOffset);
                    Thread.Sleep(1000);
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task ProcessMessageAsync(string payload, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<BookingLifecycleEvent>(payload, JsonOptions)
            ?? throw new JsonException("Пустое сообщение");

        switch (message.Type)
        {
            case BookingEventType.Confirmed:
                await HandleConfirmedAsync(message, cancellationToken);
                break;
            case BookingEventType.Cancelled:
                await HandleCancelledAsync(message, cancellationToken);
                break;
            case BookingEventType.Rejected:
                // публикует Events, читает Bookings — здесь игнорируем
                break;
            default:
                _logger.LogWarning("Неизвестный тип сообщения {Type}", message.Type);
                break;
        }
    }

    private async Task HandleConfirmedAsync(BookingLifecycleEvent message, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var processedBookings = scope.ServiceProvider.GetRequiredService<IProcessedBookingRepository>();
        var processedCancellations = scope.ServiceProvider.GetRequiredService<IProcessedCancellationRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        if (await processedBookings.ExistsAsync(message.BookingId, cancellationToken))
        {
            _logger.LogInformation("Confirm {BookingId} уже обработан", message.BookingId);
            return;
        }

        // отмена уже прошла раньше в том же топике — места не списываем
        if (await processedCancellations.ExistsAsync(message.BookingId, cancellationToken))
        {
            _logger.LogInformation(
                "Confirm {BookingId}: отмена уже обработана — пропускаем списание",
                message.BookingId);
            await processedBookings.AddAsync(ProcessedBooking.Create(message.BookingId), cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var eventItem = await eventRepository.GetByIdAsync(message.EventId, cancellationToken);
        if (eventItem is null)
        {
            await PublishRejectedAsync(message, "Событие не найдено", cancellationToken);
            await processedBookings.AddAsync(ProcessedBooking.Create(message.BookingId), cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        if (eventItem.StartAt <= DateTime.UtcNow)
        {
            await PublishRejectedAsync(message, "Событие уже началось", cancellationToken);
            await processedBookings.AddAsync(ProcessedBooking.Create(message.BookingId), cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        if (!eventItem.TryReserveSeats(message.Seats))
        {
            await PublishRejectedAsync(message, "Нет свободных мест", cancellationToken);
            await processedBookings.AddAsync(ProcessedBooking.Create(message.BookingId), cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        await processedBookings.AddAsync(ProcessedBooking.Create(message.BookingId), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // места изменились — сбрасываем кеш события (топ живёт по TTL)
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
        await cache.RemoveAsync(CacheKeys.Event(message.EventId), cancellationToken);

        _logger.LogInformation(
            "Списаны места по {BookingId}, событие {EventId}, available={Available}",
            message.BookingId,
            message.EventId,
            eventItem.AvailableSeats);
    }

    private async Task HandleCancelledAsync(BookingLifecycleEvent message, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var processedBookings = scope.ServiceProvider.GetRequiredService<IProcessedBookingRepository>();
        var processedCancellations = scope.ServiceProvider.GetRequiredService<IProcessedCancellationRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        if (await processedCancellations.ExistsAsync(message.BookingId, cancellationToken))
        {
            _logger.LogInformation("Cancel {BookingId} уже обработан", message.BookingId);
            return;
        }

        var seatsReleased = false;
        if (await processedBookings.ExistsAsync(message.BookingId, cancellationToken))
        {
            var eventItem = await eventRepository.GetByIdAsync(message.EventId, cancellationToken);
            if (eventItem is not null)
            {
                eventItem.ReleaseSeats(message.Seats);
                seatsReleased = true;
                _logger.LogInformation(
                    "Возвращены места по отмене {BookingId}, available={Available}",
                    message.BookingId,
                    eventItem.AvailableSeats);
            }
        }
        else
        {
            _logger.LogInformation(
                "Cancel {BookingId}: Confirm ещё не списывал места",
                message.BookingId);
        }

        await processedCancellations.AddAsync(
            ProcessedCancellation.Create(message.BookingId),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (seatsReleased)
        {
            var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
            await cache.RemoveAsync(CacheKeys.Event(message.EventId), cancellationToken);
        }
    }

    private Task PublishRejectedAsync(
        BookingLifecycleEvent original,
        string reason,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Rejected для брони {BookingId}: {Reason}",
            original.BookingId,
            reason);

        return _publisher.PublishAsync(
            new BookingLifecycleEvent(
                BookingEventType.Rejected,
                original.BookingId,
                original.EventId,
                original.UserId,
                original.Seats,
                DateTime.UtcNow,
                reason),
            cancellationToken);
    }
}
