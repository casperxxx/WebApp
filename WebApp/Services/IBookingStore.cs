using WebApp.Models;

namespace WebApp.Services;

public interface IBookingStore
{
    List<Booking> Bookings { get; }
}
