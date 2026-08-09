using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Хранилище броней в памяти
/// </summary>
public class InMemoryBookingStore : IBookingStore
{
    /// <summary>
    /// Список всех броней
    /// </summary>
    public List<Booking> Bookings { get; } = [];
}
