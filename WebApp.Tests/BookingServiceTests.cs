using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.DataAccess;
using WebApp.Exceptions;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Tests;

// Тесты для BookingService
public class BookingServiceTests : IDisposable
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    private readonly ServiceProvider _serviceProvider;

    public BookingServiceTests()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_dbName));
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    private async Task<Event> CreateTestEventAsync(IServiceScope scope, string title = "Тест", int totalSeats = 10)
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var eventItem = Event.Create(
            title,
            null,
            new DateTime(2026, 8, 10, 10, 0, 0),
            new DateTime(2026, 8, 10, 12, 0, 0),
            totalSeats);

        context.Events.Add(eventItem);
        await context.SaveChangesAsync();

        return eventItem;
    }

    [Fact]
    public async Task CreateBooking_ForExistingEvent_ReturnsPending()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var eventItem = await CreateTestEventAsync(scope);

        var booking = await bookingService.CreateBookingAsync(eventItem.Id);

        Assert.NotEqual(Guid.Empty, booking.Id);
        Assert.Equal(eventItem.Id, booking.EventId);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Null(booking.ProcessedAt);
        Assert.Single(context.Bookings);
    }

    [Fact]
    public async Task CreateBooking_DecreasesAvailableSeats()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var eventItem = await CreateTestEventAsync(scope, totalSeats: 5);

        await bookingService.CreateBookingAsync(eventItem.Id);

        var updated = await context.Events.FindAsync(eventItem.Id);
        Assert.Equal(4, updated!.AvailableSeats);
    }

    [Fact]
    public async Task CreateBooking_MultipleUntilLimit_AllSucceedWithUniqueIds()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var eventItem = await CreateTestEventAsync(scope, totalSeats: 3);

        var booking1 = await bookingService.CreateBookingAsync(eventItem.Id);
        var booking2 = await bookingService.CreateBookingAsync(eventItem.Id);
        var booking3 = await bookingService.CreateBookingAsync(eventItem.Id);

        Assert.Equal(3, context.Bookings.Count());
        var updated = await context.Events.FindAsync(eventItem.Id);
        Assert.Equal(0, updated!.AvailableSeats);
        Assert.NotEqual(booking1.Id, booking2.Id);
        Assert.NotEqual(booking1.Id, booking3.Id);
        Assert.NotEqual(booking2.Id, booking3.Id);
        Assert.All(context.Bookings, b => Assert.Equal(BookingStatus.Pending, b.Status));
    }

    [Fact]
    public async Task CreateBooking_NoSeats_ThrowsNoAvailableSeatsException()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var eventItem = await CreateTestEventAsync(scope, totalSeats: 1);
        await bookingService.CreateBookingAsync(eventItem.Id);

        await Assert.ThrowsAsync<NoAvailableSeatsException>(
            () => bookingService.CreateBookingAsync(eventItem.Id));

        var updated = await context.Events.FindAsync(eventItem.Id);
        Assert.Equal(0, updated!.AvailableSeats);
        Assert.Single(context.Bookings);
    }

    [Fact]
    public async Task CreateBooking_MultipleForSameEvent_UniqueIds()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var eventItem = await CreateTestEventAsync(scope);

        var booking1 = await bookingService.CreateBookingAsync(eventItem.Id);
        var booking2 = await bookingService.CreateBookingAsync(eventItem.Id);
        var booking3 = await bookingService.CreateBookingAsync(eventItem.Id);

        Assert.Equal(3, context.Bookings.Count());
        Assert.NotEqual(booking1.Id, booking2.Id);
        Assert.NotEqual(booking1.Id, booking3.Id);
        Assert.NotEqual(booking2.Id, booking3.Id);
        Assert.All(context.Bookings, b => Assert.Equal(eventItem.Id, b.EventId));
        Assert.All(context.Bookings, b => Assert.Equal(BookingStatus.Pending, b.Status));
    }

    [Fact]
    public async Task GetBookingById_ReturnsCorrectBooking()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventItem = await CreateTestEventAsync(scope);
        var created = await bookingService.CreateBookingAsync(eventItem.Id);

        var booking = await bookingService.GetBookingByIdAsync(created.Id);

        Assert.Equal(created.Id, booking.Id);
        Assert.Equal(eventItem.Id, booking.EventId);
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    [Fact]
    public async Task Confirm_SetsConfirmedStatusAndProcessedAt()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventItem = await CreateTestEventAsync(scope);
        var created = await bookingService.CreateBookingAsync(eventItem.Id);

        created.Confirm();

        Assert.Equal(BookingStatus.Confirmed, created.Status);
        Assert.NotNull(created.ProcessedAt);
    }

    [Fact]
    public async Task Reject_SetsRejectedStatusAndProcessedAt()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventItem = await CreateTestEventAsync(scope);
        var created = await bookingService.CreateBookingAsync(eventItem.Id);

        created.Reject();

        Assert.Equal(BookingStatus.Rejected, created.Status);
        Assert.NotNull(created.ProcessedAt);
    }

    [Fact]
    public async Task Reject_AndReleaseSeats_RestoresAvailableSeat()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var eventItem = await CreateTestEventAsync(scope, totalSeats: 1);
        var booking = await bookingService.CreateBookingAsync(eventItem.Id);

        var loadedEvent = await context.Events.FindAsync(eventItem.Id);
        Assert.Equal(0, loadedEvent!.AvailableSeats);

        booking.Reject();
        loadedEvent.ReleaseSeats();
        await context.SaveChangesAsync();

        var updated = await context.Events.FindAsync(eventItem.Id);
        Assert.Equal(1, updated!.AvailableSeats);
    }

    [Fact]
    public async Task Reject_AndReleaseSeats_AllowsNewBooking()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var eventItem = await CreateTestEventAsync(scope, totalSeats: 1);
        var first = await bookingService.CreateBookingAsync(eventItem.Id);

        first.Reject();
        var loadedEvent = await context.Events.FindAsync(eventItem.Id);
        loadedEvent!.ReleaseSeats();
        await context.SaveChangesAsync();

        var second = await bookingService.CreateBookingAsync(eventItem.Id);

        Assert.Equal(BookingStatus.Pending, second.Status);
        var updated = await context.Events.FindAsync(eventItem.Id);
        Assert.Equal(0, updated!.AvailableSeats);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task CreateBooking_EventNotFound()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        await Assert.ThrowsAsync<NotFoundException>(
            () => bookingService.CreateBookingAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateBooking_DeletedEvent_NotFound()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventItem = await CreateTestEventAsync(scope);

        await eventService.DeleteEventAsync(eventItem.Id);

        await Assert.ThrowsAsync<NotFoundException>(
            () => bookingService.CreateBookingAsync(eventItem.Id));
    }

    [Fact]
    public async Task GetBookingById_NotFound()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        await Assert.ThrowsAsync<NotFoundException>(
            () => bookingService.GetBookingByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateBooking_Concurrent_PreventsOverbooking()
    {
        using var seedScope = _serviceProvider.CreateScope();
        var eventItem = await CreateTestEventAsync(seedScope, totalSeats: 5);
        const int requestCount = 20;

        var tasks = Enumerable.Range(0, requestCount)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                try
                {
                    var booking = await bookingService.CreateBookingAsync(eventItem.Id);
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

        using var assertScope = _serviceProvider.CreateScope();
        var context = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await context.Events.FindAsync(eventItem.Id);
        Assert.Equal(0, updated!.AvailableSeats);
        Assert.Equal(5, context.Bookings.Count());
        Assert.Equal(5, successes.Select(r => r.Booking!.Id).Distinct().Count());
    }

    [Fact]
    public async Task CreateBooking_Concurrent_UniqueIds()
    {
        using var seedScope = _serviceProvider.CreateScope();
        var eventItem = await CreateTestEventAsync(seedScope, totalSeats: 10);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                return await bookingService.CreateBookingAsync(eventItem.Id);
            }))
            .ToArray();

        var bookings = await Task.WhenAll(tasks);

        Assert.Equal(10, bookings.Length);
        Assert.Equal(10, bookings.Select(b => b.Id).Distinct().Count());

        using var assertScope = _serviceProvider.CreateScope();
        var context = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await context.Events.FindAsync(eventItem.Id);
        Assert.Equal(0, updated!.AvailableSeats);
        Assert.All(bookings, b => Assert.Equal(BookingStatus.Pending, b.Status));
    }
}
