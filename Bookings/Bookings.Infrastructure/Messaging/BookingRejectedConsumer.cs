using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Bookings.Application.Repositories;
using Bookings.Domain.Entities;
using Shared.Contracts;

namespace Bookings.Infrastructure.Messaging;

/// <summary>
/// Читает Rejected из booking-events и отклоняет бронь в Bookings
/// </summary>
internal sealed class BookingRejectedConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BookingRejectedConsumer> _logger;

    public BookingRejectedConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<BookingRejectedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);

    private void ConsumeLoop(CancellationToken stoppingToken)
    {
        var bootstrapServers = _configuration["Kafka:BootstrapServers"];
        var groupId = _configuration["Kafka:RejectedConsumerGroup"] ?? "bookings-service-rejected";

        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            _logger.LogWarning("Kafka:BootstrapServers не задан — Rejected consumer не запускаем");
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
        _logger.LogInformation("Подписка на {Topic} (Rejected), group={Group}", KafkaTopics.BookingEvents, groupId);

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
                    _logger.LogError(ex, "Ошибка чтения Rejected из Kafka");
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Битый JSON Rejected — коммитим и пропускаем");
                    if (result is not null)
                    {
                        consumer.Commit(result);
                    }
                }
                catch (Exception ex) when (result is not null)
                {
                    // временная ошибка (БД) — не коммитим, откатываем смещение
                    _logger.LogError(ex, "Ошибка обработки Rejected — Seek и повтор");
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

        if (message.Type != BookingEventType.Rejected)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var booking = await bookingRepository.GetByIdAsync(message.BookingId, cancellationToken);
        if (booking is null)
        {
            _logger.LogWarning("Rejected для неизвестной брони {BookingId}", message.BookingId);
            return;
        }

        if (booking.Status is BookingStatus.Rejected or BookingStatus.Cancelled)
        {
            return;
        }

        booking.Reject();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Бронь {BookingId} отклонена по Rejected: {Reason}",
            message.BookingId,
            message.Reason);
    }
}
