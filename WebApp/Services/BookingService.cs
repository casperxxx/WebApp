using WebApp.Exceptions;
using WebApp.Models;

namespace WebApp.Services;

public class BookingService : IBookingService
{
    private readonly IBookingStore _bookingStore;
    private readonly IEventStore _eventStore;

    public BookingService(IBookingStore bookingStore, IEventStore eventStore)
    {
        _bookingStore = bookingStore;
        _eventStore = eventStore;
    }

    public Task<Booking> CreateBookingAsync(Guid eventId)
    {
        var eventItem = _eventStore.Events.FirstOrDefault(e => e.Id == eventId);
        if (eventItem is null)
        {
            throw new NotFoundException($"Событие с id {eventId} не найдено");
        }

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = null
        };

        _bookingStore.Bookings.Add(booking);

        return Task.FromResult(booking);
    }

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
