using WebApp.Models;

namespace WebApp.Services;

public class InMemoryBookingStore : IBookingStore
{
    public List<Booking> Bookings { get; } = [];
}
