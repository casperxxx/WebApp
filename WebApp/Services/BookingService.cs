using Microsoft.EntityFrameworkCore;
using WebApp.DataAccess;
using WebApp.Exceptions;
using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Сервис для работы с бронированиями
/// </summary>
internal class BookingService : IBookingService
{
    private static readonly SemaphoreSlim BookingLock = new(1, 1);

    private readonly AppDbContext _context;

    public BookingService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Создаёт бронь в статусе Pending
    /// </summary>
    public async Task<Booking> CreateBookingAsync(Guid eventId)
    {
        await BookingLock.WaitAsync();
        try
        {
            var eventItem = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (eventItem is null)
            {
                throw new NotFoundException($"Событие с id {eventId} не найдено");
            }

            if (!eventItem.TryReserveSeats())
            {
                throw new NoAvailableSeatsException($"Нет свободных мест для события {eventItem.Title} c id:{eventId}");
            }

            var booking = Booking.CreatePending(eventId);
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

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
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking is null)
        {
            throw new NotFoundException($"Бронь с id {bookingId} не найдена");
        }

        return booking;
    }
}
