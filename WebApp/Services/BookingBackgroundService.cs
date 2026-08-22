using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Фоновая обработка броней: Pending -> Confirmed / Rejected
/// </summary>
public class BookingBackgroundService : BackgroundService
{
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(1);

    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);
    private readonly IBookingStore _bookingStore;
    private readonly IEventStore _eventStore;
    private readonly ILogger<BookingBackgroundService> _logger;

    public BookingBackgroundService(
        IBookingStore bookingStore,
        IEventStore eventStore,
        ILogger<BookingBackgroundService> logger)
    {
        _bookingStore = bookingStore;
        _eventStore = eventStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var pendingBookings = _bookingStore.GetPending().ToList();
                var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, stoppingToken));
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

    private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(ProcessingDelay, stoppingToken);

            await _processingSemaphore.WaitAsync(stoppingToken);
            try
            {
                var eventItem = _eventStore.Events.FirstOrDefault(e => e.Id == booking.EventId);
                if (eventItem is null)
                {
                    booking.Reject();
                    _bookingStore.Update(booking);
                    _logger.LogWarning(
                        "Бронь {BookingId} отклонена: событие {EventId} не найдено",
                        booking.Id,
                        booking.EventId);
                    return;
                }

                booking.Confirm();
                _bookingStore.Update(booking);
                _logger.LogInformation("Бронь {BookingId} обработана, статус Confirmed", booking.Id);
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке брони {BookingId}", booking.Id);

            try
            {
                await _processingSemaphore.WaitAsync(stoppingToken);
                try
                {
                    booking.Reject();

                    var eventItem = _eventStore.Events.FirstOrDefault(e => e.Id == booking.EventId);
                    eventItem?.ReleaseSeats();

                    _bookingStore.Update(booking);
                }
                finally
                {
                    _processingSemaphore.Release();
                }
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "Не удалось отклонить бронь {BookingId} после ошибки", booking.Id);
            }
        }
    }
}
