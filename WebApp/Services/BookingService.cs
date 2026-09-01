using WebApp.Exceptions;
using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Сервис для работы с бронированиями
/// </summary>
public class BookingService : IBookingService
{
    private readonly object _bookingLock = new();
    private readonly IBookingStore _bookingStore;
    private readonly IEventStore _eventStore;

    public BookingService(IBookingStore bookingStore, IEventStore eventStore)
    {
        _bookingStore = bookingStore;
        _eventStore = eventStore;
    }

    /// <summary>
    /// Создаёт бронь в статусе Pending
    /// </summary>
    public Task<Booking> CreateBookingAsync(Guid eventId)
    {
        lock (_bookingLock)
        {
            var eventItem = _eventStore.Events.FirstOrDefault(e => e.Id == eventId);
            if (eventItem is null)
            {
                throw new NotFoundException($"Событие с id {eventId} не найдено");
            }

            if (!eventItem.TryReserveSeats())
            {
                throw new NoAvailableSeatsException($"Нет свободных мест для события {eventItem.Title} c id:{eventId}");
            }

            var booking = Booking.CreatePending(eventId);

            _bookingStore.Bookings.Add(booking);

            return Task.FromResult(booking);
        }
    }

    /// <summary>
    /// Возвращает бронь по Id
    /// </summary>
    public Task<Booking> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = _bookingStore.Bookings.FirstOrDefault(b => b.Id == bookingId);
        if (booking is null)
        {
            throw new NotFoundException($"Бронь с id {bookingId} не найдена");
        }

        return Task.FromResult(booking);
    }
}
