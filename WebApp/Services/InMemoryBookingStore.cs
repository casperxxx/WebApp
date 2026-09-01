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

    /// <summary>
    /// Возвращает брони в статусе Pending
    /// </summary>
    public IEnumerable<Booking> GetPending()
    {
        return Bookings.Where(b => b.Status == BookingStatus.Pending).ToList();
    }

    /// <summary>
    /// Сохраняет изменения брони
    /// </summary>
    public void Update(Booking booking)
    {
        var index = Bookings.FindIndex(b => b.Id == booking.Id);
        if (index == -1)
        {
            return;
        }

        Bookings[index] = booking;
    }
}
