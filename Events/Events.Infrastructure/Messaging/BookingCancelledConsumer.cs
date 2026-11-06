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
/// Читает BookingCancelled и возвращает AvailableSeats (если места уже списывали)
/// </summary>
internal sealed class BookingCancelledConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BookingCancelledConsumer> _logger;

    public BookingCancelledConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<BookingCancelledConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
    }

    private void ConsumeLoop(CancellationToken stoppingToken)
    {
        var bootstrapServers = _configuration["Kafka:BootstrapServers"];
        var groupId = _configuration["Kafka:CancellationConsumerGroup"]
            ?? "events-service-cancellations";

        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            _logger.LogWarning("Kafka:BootstrapServers не задан — consumer отмен не запускаем");
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
        consumer.Subscribe(KafkaTopics.BookingCancelled);
        _logger.LogInformation(
            "Подписка на {Topic}, group={Group}",
            KafkaTopics.BookingCancelled,
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
                    _logger.LogError(ex, "Ошибка чтения BookingCancelled из Kafka");
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка обработки сообщения BookingCancelled");
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
        BookingCancelled? message;
        try
        {
            message = JsonSerializer.Deserialize<BookingCancelled>(payload, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось разобрать BookingCancelled: {Payload}", payload);
            return;
        }

        if (message is null)
        {
            _logger.LogWarning("Пустое сообщение BookingCancelled");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var processedBookings = scope.ServiceProvider.GetRequiredService<IProcessedBookingRepository>();
        var processedCancellations = scope.ServiceProvider.GetRequiredService<IProcessedCancellationRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        if (await processedCancellations.ExistsAsync(message.BookingId, cancellationToken))
        {
            _logger.LogInformation(
                "Отмена брони {BookingId} уже обработана — пропускаем",
                message.BookingId);
            return;
        }

        // места возвращаем только если раньше списывали по Confirm
        var seatsWereReserved = await processedBookings.ExistsAsync(message.BookingId, cancellationToken);
        if (seatsWereReserved)
        {
            var eventItem = await eventRepository.GetByIdAsync(message.EventId, cancellationToken);
            if (eventItem is null)
            {
                _logger.LogWarning(
                    "Событие {EventId} не найдено для отмены брони {BookingId}",
                    message.EventId,
                    message.BookingId);
            }
            else
            {
                eventItem.ReleaseSeats(message.Seats);
                _logger.LogInformation(
                    "Возвращены места по отмене {BookingId}, событие {EventId}, available={Available}",
                    message.BookingId,
                    message.EventId,
                    eventItem.AvailableSeats);
            }
        }
        else
        {
            _logger.LogInformation(
                "Отмена {BookingId}: Confirm ещё не списывал места — ReleaseSeats не нужен",
                message.BookingId);
        }

        await processedCancellations.AddAsync(
            ProcessedCancellation.Create(message.BookingId),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
