using Events.Domain.Entities;
using Events.Domain.Exceptions;

namespace Events.Tests;

public class EventDomainTests
{
    private static Event CreateEvent(int seats = 5) =>
        Event.Create(
            "Test",
            null,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(2),
            seats);

    [Fact]
    public void TryReserveSeats_DecreasesAvailable()
    {
        var ev = CreateEvent(3);

        Assert.True(ev.TryReserveSeats(1));
        Assert.Equal(2, ev.AvailableSeats);
    }

    [Fact]
    public void TryReserveSeats_WhenNotEnough_ReturnsFalse()
    {
        var ev = CreateEvent(1);
        ev.TryReserveSeats(1);

        Assert.False(ev.TryReserveSeats(1));
        Assert.Equal(0, ev.AvailableSeats);
    }

    [Fact]
    public void ReleaseSeats_RestoresUpToTotal()
    {
        var ev = CreateEvent(2);
        ev.TryReserveSeats(1);
        ev.ReleaseSeats(1);

        Assert.Equal(2, ev.AvailableSeats);
    }

    [Fact]
    public void ReleaseSeats_DoesNotExceedTotal()
    {
        var ev = CreateEvent(2);
        ev.ReleaseSeats(5);

        Assert.Equal(2, ev.AvailableSeats);
    }

    [Fact]
    public void Create_InvalidDates_Throws()
    {
        Assert.Throws<DomainValidationException>(() =>
            Event.Create("x", null, DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(1), 10));
    }

    [Fact]
    public void ProcessedBooking_Create_SetsUtc()
    {
        var id = Guid.NewGuid();
        var processed = ProcessedBooking.Create(id);

        Assert.Equal(id, processed.BookingId);
        Assert.True(processed.ProcessedAtUtc <= DateTime.UtcNow.AddSeconds(1));
    }
}
