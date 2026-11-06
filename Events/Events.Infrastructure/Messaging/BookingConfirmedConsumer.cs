using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Events.Application.Repositories;
using Events.Domain.Entities;
using Shared.Contracts;

namespace Events.Infrastructure.Messaging;

/// <summary>
/// Читает BookingConfirmed и уменьшает AvailableSeats у события
/// </summary>
internal sealed class BookingConfirmedConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BookingConfirmedConsumer> _logger;

    public BookingConfirmedConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<BookingConfirmedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Consume блокирующий — крутим в отдельном потоке пула
        return Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
    }

    private void ConsumeLoop(CancellationToken stoppingToken)
    {
        var bootstrapServers = _configuration["Kafka:BootstrapServers"];
        var groupId = _configuration["Kafka:ConsumerGroup"] ?? "events-service";

        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            _logger.LogWarning("Kafka:BootstrapServers не задан — consumer не запускаем");
            return;
        }

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(KafkaTopics.BookingConfirmed);
        _logger.LogInformation(
            "Подписка на {Topic}, group={Group}",
            KafkaTopics.BookingConfirmed,
            groupId);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    if (result?.Message?.Value is null)
                    {
                        continue;
                    }

                    ProcessMessageAsync(result.Message.Value, stoppingToken)
                        .GetAwaiter()
                        .GetResult();

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
                catch (Exception ex)
                {
                    // одно «плохое» сообщение не роняет весь consumer
                    _logger.LogError(ex, "Ошибка обработки сообщения BookingConfirmed");
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
        BookingConfirmed? message;
        try
        {
            message = JsonSerializer.Deserialize<BookingConfirmed>(payload, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось разобрать BookingConfirmed: {Payload}", payload);
            return;
        }

        if (message is null)
        {
            _logger.LogWarning("Пустое сообщение BookingConfirmed");
            return;
        }

        // на каждое сообщение — свой scope (DbContext scoped)
        using var scope = _scopeFactory.CreateScope();
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var processedRepository = scope.ServiceProvider.GetRequiredService<IProcessedBookingRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        if (await processedRepository.ExistsAsync(message.BookingId, cancellationToken))
        {
            _logger.LogInformation(
                "Бронь {BookingId} уже обработана — пропускаем",
                message.BookingId);
            return;
        }

        var eventItem = await eventRepository.GetByIdAsync(message.EventId, cancellationToken);
        if (eventItem is null)
        {
            _logger.LogWarning(
                "Событие {EventId} не найдено для брони {BookingId} — пропускаем",
                message.EventId,
                message.BookingId);
            return;
        }

        if (!eventItem.TryReserveSeats(message.Seats))
        {
            _logger.LogWarning(
                "Нет мест на событии {EventId} для брони {BookingId} — пропускаем",
                message.EventId,
                message.BookingId);
            return;
        }

        await processedRepository.AddAsync(ProcessedBooking.Create(message.BookingId), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Списаны места по брони {BookingId}, событие {EventId}, available={Available}",
            message.BookingId,
            message.EventId,
            eventItem.AvailableSeats);
    }
}
