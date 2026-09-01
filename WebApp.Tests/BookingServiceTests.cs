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
    private Event CreateTestEvent(string title = "Тест", int totalSeats = 10)
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
        var eventItem = CreateTestEvent();

        var booking = await _bookingService.CreateBookingAsync(eventItem.Id);

        Assert.NotEqual(Guid.Empty, booking.Id);
        Assert.Equal(eventItem.Id, booking.EventId);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Null(booking.ProcessedAt);
        Assert.Single(_bookingStore.Bookings);
    }

    // создание брони уменьшает AvailableSeats на 1
    [Fact]
    public async Task CreateBooking_DecreasesAvailableSeats()
    {
        var eventItem = CreateTestEvent(totalSeats: 5);

        await _bookingService.CreateBookingAsync(eventItem.Id);

        Assert.Equal(4, eventItem.AvailableSeats);
    }

    // несколько броней до лимита — все успешны, у каждой уникальный Id
    [Fact]
    public async Task CreateBooking_MultipleUntilLimit_AllSucceedWithUniqueIds()
    {
        var eventItem = CreateTestEvent(totalSeats: 3);

        var booking1 = await _bookingService.CreateBookingAsync(eventItem.Id);
        var booking2 = await _bookingService.CreateBookingAsync(eventItem.Id);
        var booking3 = await _bookingService.CreateBookingAsync(eventItem.Id);

        Assert.Equal(3, _bookingStore.Bookings.Count);
        Assert.Equal(0, eventItem.AvailableSeats);
        Assert.NotEqual(booking1.Id, booking2.Id);
        Assert.NotEqual(booking1.Id, booking3.Id);
        Assert.NotEqual(booking2.Id, booking3.Id);
        Assert.All(_bookingStore.Bookings, b => Assert.Equal(BookingStatus.Pending, b.Status));
    }

    // после исчерпания мест — NoAvailableSeatsException
    [Fact]
    public async Task CreateBooking_NoSeats_ThrowsNoAvailableSeatsException()
    {
        var eventItem = CreateTestEvent(totalSeats: 1);
        await _bookingService.CreateBookingAsync(eventItem.Id);

        await Assert.ThrowsAsync<NoAvailableSeatsException>(
            () => _bookingService.CreateBookingAsync(eventItem.Id));

        Assert.Equal(0, eventItem.AvailableSeats);
        Assert.Single(_bookingStore.Bookings);
    }

    // несколько броней на одно событие — у всех разные Id
    [Fact]
    public async Task CreateBooking_MultipleForSameEvent_UniqueIds()
    {
        var eventItem = CreateTestEvent();

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
        var eventItem = CreateTestEvent();
        var created = await _bookingService.CreateBookingAsync(eventItem.Id);

        var booking = await _bookingService.GetBookingByIdAsync(created.Id);

        Assert.Equal(created.Id, booking.Id);
        Assert.Equal(eventItem.Id, booking.EventId);
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    // после Confirm — Confirmed и заполненный ProcessedAt
    [Fact]
    public async Task Confirm_SetsConfirmedStatusAndProcessedAt()
    {
        var eventItem = CreateTestEvent();
        var created = await _bookingService.CreateBookingAsync(eventItem.Id);

        created.Confirm();

        var booking = await _bookingService.GetBookingByIdAsync(created.Id);

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    // после Reject — Rejected и заполненный ProcessedAt
    [Fact]
    public async Task Reject_SetsRejectedStatusAndProcessedAt()
    {
        var eventItem = CreateTestEvent();
        var created = await _bookingService.CreateBookingAsync(eventItem.Id);

        created.Reject();

        var booking = await _bookingService.GetBookingByIdAsync(created.Id);

        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    // после Reject + ReleaseSeats место возвращается
    [Fact]
    public async Task Reject_AndReleaseSeats_RestoresAvailableSeat()
    {
        var eventItem = CreateTestEvent(totalSeats: 1);
        var booking = await _bookingService.CreateBookingAsync(eventItem.Id);
        Assert.Equal(0, eventItem.AvailableSeats);

        booking.Reject();
        eventItem.ReleaseSeats();

        Assert.Equal(1, eventItem.AvailableSeats);
    }

    // после Reject + ReleaseSeats можно снова забронировать
    [Fact]
    public async Task Reject_AndReleaseSeats_AllowsNewBooking()
    {
        var eventItem = CreateTestEvent(totalSeats: 1);
        var first = await _bookingService.CreateBookingAsync(eventItem.Id);

        first.Reject();
        eventItem.ReleaseSeats();

        var second = await _bookingService.CreateBookingAsync(eventItem.Id);

        Assert.Equal(BookingStatus.Pending, second.Status);
        Assert.Equal(0, eventItem.AvailableSeats);
        Assert.NotEqual(first.Id, second.Id);
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
        var eventItem = CreateTestEvent();
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

    // 5 мест, 20 параллельных запросов — 5 успехов, 15 NoAvailableSeatsException
    [Fact]
    public async Task CreateBooking_Concurrent_PreventsOverbooking()
    {
        var eventItem = CreateTestEvent(totalSeats: 5);
        const int requestCount = 20;

        var tasks = Enumerable.Range(0, requestCount)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    var booking = await _bookingService.CreateBookingAsync(eventItem.Id);
                    return (Success: true, Booking: booking, Error: (Exception?)null);
                }
                catch (Exception ex)
                {
                    return (Success: false, Booking: (Booking?)null, Error: ex);
                }
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        var successes = results.Where(r => r.Success).ToList();
        var failures = results.Where(r => !r.Success).ToList();

        Assert.Equal(5, successes.Count);
        Assert.Equal(15, failures.Count);
        Assert.All(failures, r => Assert.IsType<NoAvailableSeatsException>(r.Error));
        Assert.Equal(0, eventItem.AvailableSeats);
        Assert.Equal(5, _bookingStore.Bookings.Count);
        Assert.Equal(5, successes.Select(r => r.Booking!.Id).Distinct().Count());
    }

    // 10 мест, 10 параллельных запросов — 10 уникальных Id
    [Fact]
    public async Task CreateBooking_Concurrent_UniqueIds()
    {
        var eventItem = CreateTestEvent(totalSeats: 10);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => _bookingService.CreateBookingAsync(eventItem.Id)))
            .ToArray();

        var bookings = await Task.WhenAll(tasks);

        Assert.Equal(10, bookings.Length);
        Assert.Equal(10, bookings.Select(b => b.Id).Distinct().Count());
        Assert.Equal(0, eventItem.AvailableSeats);
        Assert.All(bookings, b => Assert.Equal(BookingStatus.Pending, b.Status));
    }
}
