using WebApp.Exceptions;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Tests;

// Тесты для BookingService
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

    // вспомогательный метод — добавляет событие в store
    private Event AddEvent(string title = "Тест", int totalSeats = 10)
    {
        var eventItem = Event.Create(
            title,
            null,
            new DateTime(2026, 8, 10, 10, 0, 0),
            new DateTime(2026, 8, 10, 12, 0, 0),
            totalSeats);

        _eventStore.Events.Add(eventItem);
        return eventItem;
    }

    // успешное создание брони — статус должен быть Pending
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

    // несколько броней на одно событие — у всех разные Id
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

    // получение брони по Id
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

    // после Confirm get должен вернуть новый статус
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

    // то же самое для Rejected
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

    // событие не существует
    [Fact]
    public async Task CreateBooking_EventNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _bookingService.CreateBookingAsync(Guid.NewGuid()));
    }

    // событие удалили — бронировать уже нельзя
    [Fact]
    public async Task CreateBooking_DeletedEvent_NotFound()
    {
        var eventItem = AddEvent();
        _eventService.DeleteEvent(eventItem.Id);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _bookingService.CreateBookingAsync(eventItem.Id));
    }

    // брони с таким Id нет
    [Fact]
    public async Task GetBookingById_NotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _bookingService.GetBookingByIdAsync(Guid.NewGuid()));
    }
}
