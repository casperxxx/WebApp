using WebApp.Exceptions;
using WebApp.Models;
using WebApp.Repositories;

namespace WebApp.Services;

/// <summary>
/// Сервис для работы с бронированиями
/// </summary>
internal class BookingService : IBookingService
{
    private static readonly SemaphoreSlim BookingLock = new(1, 1);

    // сервисы больше не ходят в AppDbContext напрямую
    private readonly IEventRepository _eventRepository;
    private readonly IBookingRepository _bookingRepository;

    public BookingService(IEventRepository eventRepository, IBookingRepository bookingRepository)
    {
        _eventRepository = eventRepository;
        _bookingRepository = bookingRepository;
    }

    /// <summary>
    /// Создаёт бронь в статусе Pending
    /// </summary>
    public async Task<Booking> CreateBookingAsync(Guid eventId)
    {
        await BookingLock.WaitAsync();
        try
        {
            var eventItem = await _eventRepository.GetByIdAsync(eventId);
            if (eventItem is null)
            {
                throw new NotFoundException($"Событие с id {eventId} не найдено");
            }

            if (!eventItem.TryReserveSeats())
            {
                throw new NoAvailableSeatsException($"Нет свободных мест для события {eventItem.Title} c id:{eventId}");
            }

            var booking = Booking.CreatePending(eventId);
            // AddAsync сохранит и бронь, и уменьшенные AvailableSeats
            await _bookingRepository.AddAsync(booking);

            return booking;
        }
        finally
        {
            BookingLock.Release();
        }
    }

    /// <summary>
    /// Возвращает бронь по Id
    /// </summary>
    public async Task<Booking> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking is null)
        {
            throw new NotFoundException($"Бронь с id {bookingId} не найдена");
        }

        return booking;
    }
}
