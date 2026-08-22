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

    /// <summary>
    /// Возвращает брони в статусе Pending
    /// </summary>
    IEnumerable<Booking> GetPending();

    /// <summary>
    /// Сохраняет изменения брони
    /// </summary>
    void Update(Booking booking);
}
