using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Bookings.Application.Messaging;
using Bookings.Application.Repositories;
using Shared.Contracts;

namespace Bookings.Application.Services;

/// <summary>
/// Читает outbox и публикует сообщения в Kafka по порядку CreatedAt
/// </summary>
public sealed class OutboxPublisherService : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(1);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IBookingEventPublisher _publisher;
    private readonly ILogger<OutboxPublisherService> _logger;

    public OutboxPublisherService(
        IServiceScopeFactory scopeFactory,
        IBookingEventPublisher publisher,
        ILogger<OutboxPublisherService> logger)
    {
        _scopeFactory = scopeFactory;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка публикации outbox");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task PublishPendingAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var pending = await outboxRepository.GetPendingAsync(50, stoppingToken);
        foreach (var message in pending)
        {
            var lifecycleEvent = JsonSerializer.Deserialize<BookingLifecycleEvent>(
                message.PayloadJson,
                JsonOptions);

            if (lifecycleEvent is null)
            {
                _logger.LogError("Невалидный payload outbox {OutboxId}", message.Id);
                message.MarkPublished();
                await unitOfWork.SaveChangesAsync(stoppingToken);
                continue;
            }

            // при ошибке Kafka не помечаем — повторим на следующем цикле (то же сообщение)
            await _publisher.PublishAsync(lifecycleEvent, stoppingToken);
            message.MarkPublished();
            await unitOfWork.SaveChangesAsync(stoppingToken);

            _logger.LogInformation(
                "Outbox {OutboxId} отправлен: {Type} BookingId={BookingId}",
                message.Id,
                lifecycleEvent.Type,
                lifecycleEvent.BookingId);
        }
    }
}
