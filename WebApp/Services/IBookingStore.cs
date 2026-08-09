using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Хранилище броней
/// </summary>
public interface IBookingStore
{
    /// <summary>
    /// Список всех броней
    /// </summary>
    List<Booking> Bookings { get; }
}
