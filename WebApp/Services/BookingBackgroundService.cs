using WebApp.Models;

namespace WebApp.Services;

public class BookingBackgroundService : BackgroundService
{
    private readonly IBookingStore _bookingStore;
    private readonly ILogger<BookingBackgroundService> _logger;

    public BookingBackgroundService(IBookingStore bookingStore, ILogger<BookingBackgroundService> logger)
    {
        _bookingStore = bookingStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var pendingBookings = _bookingStore.Bookings
                    .Where(b => b.Status == BookingStatus.Pending)
                    .ToList();

                foreach (var booking in pendingBookings)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

                    booking.Status = BookingStatus.Confirmed;
                    booking.ProcessedAt = DateTime.UtcNow;

                    _logger.LogInformation("Бронь {BookingId} обработана, статус Confirmed", booking.Id);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при фоновой обработке бронирований");
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
