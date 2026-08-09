using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Сервис для работы с бронированиями
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Создать бронь для события
    /// </summary>
    /// <param name="eventId">Id события</param>
    Task<Booking> CreateBookingAsync(Guid eventId);

    /// <summary>
    /// Получить бронь по Id
    /// </summary>
    /// <param name="bookingId">Id брони</param>
    Task<Booking> GetBookingByIdAsync(Guid bookingId);
}
