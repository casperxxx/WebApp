using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Bookings.Application.Messaging;
using Bookings.Application.Repositories;
using Bookings.Domain.Entities;
using Shared.Contracts;

namespace Bookings.Application.Services;

/// <summary>
/// Фоновая обработка броней: Pending -> Confirmed / Rejected
/// </summary>
public class BookingBackgroundService : BackgroundService
{
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IBookingConfirmedPublisher _publisher;
    private readonly ILogger<BookingBackgroundService> _logger;

    public BookingBackgroundService(
        IServiceScopeFactory scopeFactory,
        IBookingConfirmedPublisher publisher,
        ILogger<BookingBackgroundService> logger)
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
                List<Guid> pendingIds;
                // scope нужен, потому что репозиторий (и DbContext) — scoped, а фон — singleton
                using (var scope = _scopeFactory.CreateScope())
                {
                    var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                    pendingIds = await bookingRepository.GetPendingIdsAsync(stoppingToken);
                }

                var tasks = pendingIds.Select(id => ProcessBookingAsync(id, stoppingToken));
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при фоновой обработке бронирований");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(ProcessingDelay, stoppingToken);

            using var scope = _scopeFactory.CreateScope();
            var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var booking = await bookingRepository.GetByIdAsync(bookingId, stoppingToken);
            if (booking is null || booking.Status != BookingStatus.Pending)
            {
                return;
            }

            // сначала Kafka, потом Confirm в БД:
            // если publish упал — остаёмся Pending и повторим на следующем цикле;
            // если publish прошёл, а Confirm нет — Events идемпотентен по BookingId
            var confirmedAt = DateTime.UtcNow;
            await _publisher.PublishAsync(
                new BookingConfirmed(
                    booking.Id,
                    booking.EventId,
                    booking.UserId,
                    Seats: 1,
                    confirmedAt),
                stoppingToken);

            booking.Confirm();
            await unitOfWork.SaveChangesAsync(stoppingToken);

            _logger.LogInformation("Бронь {BookingId} обработана, статус Confirmed", booking.Id);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке брони {BookingId}", bookingId);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var booking = await bookingRepository.GetByIdAsync(bookingId, stoppingToken);
                if (booking is null || booking.Status != BookingStatus.Pending)
                {
                    return;
                }

                booking.Reject();
                await unitOfWork.SaveChangesAsync(stoppingToken);
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "Не удалось отклонить бронь {BookingId} после ошибки", bookingId);
            }
        }
    }
}
