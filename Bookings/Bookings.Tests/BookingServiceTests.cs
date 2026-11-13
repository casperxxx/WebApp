using Bookings.Application.Repositories;
using Bookings.Application.Services;
using Bookings.Domain.Entities;
using Bookings.Domain.Exceptions;
using Moq;

namespace Bookings.Tests;

public class BookingServiceTests
{
    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly Mock<IOutboxRepository> _outbox = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly BookingService _sut;

    public BookingServiceTests()
    {
        _sut = new BookingService(_bookings.Object, _outbox.Object, _uow.Object);
    }

    [Fact]
    public async Task CreateBooking_WhenLimitExceeded_Throws409Style()
    {
        var userId = Guid.NewGuid();
        _bookings.Setup(r => r.CountActiveByUserAsync(userId, default))
            .ReturnsAsync(BookingService.MaxActiveBookings);

        await Assert.ThrowsAsync<BookingLimitExceededException>(() =>
            _sut.CreateBookingAsync(Guid.NewGuid(), userId));
    }

    [Fact]
    public async Task CreateBooking_AddsPendingWithDefaultSeats()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        _bookings.Setup(r => r.CountActiveByUserAsync(userId, default)).ReturnsAsync(0);
        Booking? saved = null;
        _bookings.Setup(r => r.AddAsync(It.IsAny<Booking>(), default))
            .Callback<Booking, CancellationToken>((b, _) => saved = b)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateBookingAsync(eventId, userId);

        Assert.NotNull(saved);
        Assert.Equal(Booking.DefaultSeats, result.Seats);
        Assert.Equal(BookingStatus.Pending, result.Status);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task GetBooking_ForeignUser_ThrowsForbidden()
    {
        var booking = Booking.CreatePending(Guid.NewGuid(), Guid.NewGuid());
        _bookings.Setup(r => r.GetByIdAsync(booking.Id, default)).ReturnsAsync(booking);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.GetBookingByIdAsync(booking.Id, Guid.NewGuid(), isAdmin: false));
    }

    [Fact]
    public async Task Cancel_Confirmed_WritesOutbox()
    {
        var owner = Guid.NewGuid();
        var booking = Booking.CreatePending(Guid.NewGuid(), owner);
        booking.Confirm();
        _bookings.Setup(r => r.GetByIdAsync(booking.Id, default)).ReturnsAsync(booking);

        await _sut.CancelBookingAsync(booking.Id, owner, isAdmin: false);

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        _outbox.Verify(o => o.AddAsync(It.IsAny<OutboxMessage>(), default), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Cancel_Pending_DoesNotWriteOutbox()
    {
        var owner = Guid.NewGuid();
        var booking = Booking.CreatePending(Guid.NewGuid(), owner);
        _bookings.Setup(r => r.GetByIdAsync(booking.Id, default)).ReturnsAsync(booking);

        await _sut.CancelBookingAsync(booking.Id, owner, isAdmin: false);

        _outbox.Verify(o => o.AddAsync(It.IsAny<OutboxMessage>(), default), Times.Never);
    }
}
