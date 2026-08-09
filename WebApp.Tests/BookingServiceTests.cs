using WebApp.Exceptions;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Tests;

public class BookingServiceTests
{
    private readonly InMemoryEventStore _eventStore = new();
    private readonly InMemoryBookingStore _bookingStore = new();
    private readonly EventService _eventService;
    private readonly BookingService _bookingService;

    public BookingServiceTests()
    {
        _eventService = new EventService(_eventStore);
        _bookingService = new BookingService(_bookingStore, _eventStore);
    }

    private Event AddEvent(string title = "Тест")
    {
        var eventItem = new Event
        {
            Title = title,
            StartAt = new DateTime(2026, 8, 10, 10, 0, 0),
            EndAt = new DateTime(2026, 8, 10, 12, 0, 0)
        };

        _eventService.AddEvent(eventItem);
        return eventItem;
    }

    [Fact]
    public async Task CreateBooking_ForExistingEvent_ReturnsPending()
    {
        var eventItem = AddEvent();

        var booking = await _bookingService.CreateBookingAsync(eventItem.Id);

        Assert.NotEqual(Guid.Empty, booking.Id);
        Assert.Equal(eventItem.Id, booking.EventId);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Null(booking.ProcessedAt);
        Assert.Single(_bookingStore.Bookings);
    }

    [Fact]
    public async Task CreateBooking_MultipleForSameEvent_UniqueIds()
    {
        var eventItem = AddEvent();

        var booking1 = await _bookingService.CreateBookingAsync(eventItem.Id);
        var booking2 = await _bookingService.CreateBookingAsync(eventItem.Id);
        var booking3 = await _bookingService.CreateBookingAsync(eventItem.Id);

        Assert.Equal(3, _bookingStore.Bookings.Count);
        Assert.NotEqual(booking1.Id, booking2.Id);
        Assert.NotEqual(booking1.Id, booking3.Id);
        Assert.NotEqual(booking2.Id, booking3.Id);
        Assert.All(_bookingStore.Bookings, b => Assert.Equal(eventItem.Id, b.EventId));
        Assert.All(_bookingStore.Bookings, b => Assert.Equal(BookingStatus.Pending, b.Status));
    }

    [Fact]
    public async Task GetBookingById_ReturnsCorrectBooking()
    {
        var eventItem = AddEvent();
        var created = await _bookingService.CreateBookingAsync(eventItem.Id);

        var booking = await _bookingService.GetBookingByIdAsync(created.Id);

        Assert.Equal(created.Id, booking.Id);
        Assert.Equal(eventItem.Id, booking.EventId);
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    [Fact]
    public async Task GetBookingById_ReflectsConfirmedStatus()
    {
        var eventItem = AddEvent();
        var created = await _bookingService.CreateBookingAsync(eventItem.Id);

        created.Status = BookingStatus.Confirmed;
        created.ProcessedAt = DateTime.UtcNow;

        var booking = await _bookingService.GetBookingByIdAsync(created.Id);

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    [Fact]
    public async Task GetBookingById_ReflectsRejectedStatus()
    {
        var eventItem = AddEvent();
        var created = await _bookingService.CreateBookingAsync(eventItem.Id);

        created.Status = BookingStatus.Rejected;
        created.ProcessedAt = DateTime.UtcNow;

        var booking = await _bookingService.GetBookingByIdAsync(created.Id);

        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    [Fact]
    public async Task CreateBooking_EventNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _bookingService.CreateBookingAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateBooking_DeletedEvent_NotFound()
    {
        var eventItem = AddEvent();
        _eventService.DeleteEvent(eventItem.Id);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _bookingService.CreateBookingAsync(eventItem.Id));
    }

    [Fact]
    public async Task GetBookingById_NotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _bookingService.GetBookingByIdAsync(Guid.NewGuid()));
    }
}
