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
    /// <param name="userId">Id пользователя, который бронирует</param>
    Task<BookingResponse> CreateBookingAsync(Guid eventId, Guid userId);

    /// <summary>
    /// Получить бронь по Id
    /// </summary>
    /// <param name="bookingId">Id брони</param>
    Task<BookingResponse> GetBookingByIdAsync(Guid bookingId);

    /// <summary>
    /// Отменить бронь (владелец или администратор)
    /// </summary>
    /// <param name="bookingId">Id брони</param>
    /// <param name="currentUserId">Id текущего пользователя</param>
    /// <param name="isAdmin">Является ли текущий пользователь администратором</param>
    Task CancelBookingAsync(Guid bookingId, Guid currentUserId, bool isAdmin);
}
