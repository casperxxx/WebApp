using Bookings.Domain.Entities;
using Bookings.Domain.Exceptions;

namespace Bookings.Tests;

public class BookingDomainTests
{
    [Fact]
    public void CreatePending_UsesDefaultSeats()
    {
        var booking = Booking.CreatePending(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(Booking.DefaultSeats, booking.Seats);
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    [Fact]
    public void Cancel_Confirmed_Succeeds()
    {
        var booking = Booking.CreatePending(Guid.NewGuid(), Guid.NewGuid());
        booking.Confirm();
        booking.Cancel();

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }

    [Fact]
    public void Cancel_Rejected_Throws()
    {
        var booking = Booking.CreatePending(Guid.NewGuid(), Guid.NewGuid());
        booking.Reject();

        Assert.Throws<DomainValidationException>(() => booking.Cancel());
    }

    [Fact]
    public void Reject_Confirmed_Succeeds()
    {
        var booking = Booking.CreatePending(Guid.NewGuid(), Guid.NewGuid());
        booking.Confirm();
        booking.Reject();

        Assert.Equal(BookingStatus.Rejected, booking.Status);
    }
}
