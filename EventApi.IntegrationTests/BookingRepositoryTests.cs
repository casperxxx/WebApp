using WebApp.Models;

namespace EventApi.IntegrationTests;

[Collection("Postgres")]
public class BookingRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public BookingRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static DateTime Utc(int year, int month, int day, int hour = 10) =>
        new(year, month, day, hour, 0, 0, DateTimeKind.Utc);

    private static Event CreateEvent(string title = "Тест", int seats = 10) =>
        Event.Create(title, null, Utc(2026, 8, 10), Utc(2026, 8, 10, 12), seats);

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_ReturnsPendingBooking()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var (events, bookings, context) = _fixture.CreateRepositories();
        await using (context)
        {
            var eventItem = CreateEvent();
            await events.AddAsync(eventItem);

            var booking = Booking.CreatePending(eventItem.Id);

            // Act
            await bookings.AddAsync(booking);
            var loaded = await bookings.GetByIdAsync(booking.Id);

            // Assert
            Assert.NotNull(loaded);
            Assert.Equal(booking.Id, loaded.Id);
            Assert.Equal(eventItem.Id, loaded.EventId);
            Assert.Equal(BookingStatus.Pending, loaded.Status);
        }
    }

    [Fact]
    public async Task AddAsync_SavesAvailableSeatsChangeOnEvent()
    {
        await _fixture.ResetDatabaseAsync();
        var (events, bookings, context) = _fixture.CreateRepositories();
        await using (context)
        {
            var eventItem = CreateEvent(seats: 3);
            await events.AddAsync(eventItem);

            // загружаем снова в том же контексте после Add — нужен tracked event
            var tracked = await events.GetByIdAsync(eventItem.Id);
            Assert.True(tracked!.TryReserveSeats());

            var booking = Booking.CreatePending(tracked.Id);
            await bookings.AddAsync(booking);

            // новый контекст — проверяем, что места сохранились
            var (events2, _, context2) = _fixture.CreateRepositories();
            await using (context2)
            {
                var reloaded = await events2.GetByIdAsync(eventItem.Id);
                Assert.Equal(2, reloaded!.AvailableSeats);
            }
        }
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        await _fixture.ResetDatabaseAsync();
        var (_, bookings, context) = _fixture.CreateRepositories();
        await using (context)
        {
            var loaded = await bookings.GetByIdAsync(Guid.NewGuid());
            Assert.Null(loaded);
        }
    }

    [Fact]
    public async Task GetPendingIdsAsync_ReturnsOnlyPending()
    {
        await _fixture.ResetDatabaseAsync();
        var (events, bookings, context) = _fixture.CreateRepositories();
        await using (context)
        {
            var eventItem = CreateEvent(seats: 5);
            await events.AddAsync(eventItem);

            var pending1 = Booking.CreatePending(eventItem.Id);
            var pending2 = Booking.CreatePending(eventItem.Id);
            var confirmed = Booking.CreatePending(eventItem.Id);
            await bookings.AddAsync(pending1);
            await bookings.AddAsync(pending2);
            await bookings.AddAsync(confirmed);

            confirmed.Confirm();
            await bookings.SaveChangesAsync();

            var ids = await bookings.GetPendingIdsAsync();

            Assert.Equal(2, ids.Count);
            Assert.Contains(pending1.Id, ids);
            Assert.Contains(pending2.Id, ids);
            Assert.DoesNotContain(confirmed.Id, ids);
        }
    }

    [Fact]
    public async Task SaveChangesAsync_Confirm_UpdatesStatus()
    {
        await _fixture.ResetDatabaseAsync();
        var (events, bookings, context) = _fixture.CreateRepositories();
        await using (context)
        {
            var eventItem = CreateEvent();
            await events.AddAsync(eventItem);

            var booking = Booking.CreatePending(eventItem.Id);
            await bookings.AddAsync(booking);

            booking.Confirm();
            await bookings.SaveChangesAsync();

            var loaded = await bookings.GetByIdAsync(booking.Id);
            Assert.Equal(BookingStatus.Confirmed, loaded!.Status);
            Assert.NotNull(loaded.ProcessedAt);
        }
    }

    [Fact]
    public async Task SaveChangesAsync_Reject_AndReleaseSeats()
    {
        await _fixture.ResetDatabaseAsync();
        var (events, bookings, context) = _fixture.CreateRepositories();
        await using (context)
        {
            var eventItem = CreateEvent(seats: 1);
            await events.AddAsync(eventItem);

            var tracked = await events.GetByIdAsync(eventItem.Id);
            tracked!.TryReserveSeats();
            var booking = Booking.CreatePending(tracked.Id);
            await bookings.AddAsync(booking);

            booking.Reject();
            tracked.ReleaseSeats();
            await bookings.SaveChangesAsync();

            var loadedBooking = await bookings.GetByIdAsync(booking.Id);
            var loadedEvent = await events.GetByIdAsync(eventItem.Id);

            Assert.Equal(BookingStatus.Rejected, loadedBooking!.Status);
            Assert.Equal(1, loadedEvent!.AvailableSeats);
        }
    }
}
