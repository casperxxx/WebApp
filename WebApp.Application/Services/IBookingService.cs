using WebApp.Application.Models;

namespace WebApp.Application.Services;

/// <summary>
/// Сервис для работы с бронированиями
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Создать бронь для события
    /// </summary>
    /// <param name="eventId">Id события</param>
    Task<BookingResponse> CreateBookingAsync(Guid eventId);

    /// <summary>
    /// Получить бронь по Id
    /// </summary>
    /// <param name="bookingId">Id брони</param>
    Task<BookingResponse> GetBookingByIdAsync(Guid bookingId);
}
