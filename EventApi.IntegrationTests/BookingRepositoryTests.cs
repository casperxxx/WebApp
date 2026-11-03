using WebApp.Application.Repositories;
using WebApp.Domain.Entities;
using WebApp.Infrastructure.DataAccess;

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
        Event.Create(title, null, Utc(2027, 8, 10), Utc(2027, 8, 10, 12), seats);

    /// <summary>
    /// Пользователь нужен из‑за FK bookings → users
    /// </summary>
    private static async Task<User> AddUserAsync(AppDbContext context, IUnitOfWork unitOfWork)
    {
        var user = User.Create($"user_{Guid.NewGuid():N}", "HASH", UserRole.User);
        context.Users.Add(user);
        await unitOfWork.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_ReturnsPendingBooking()
    {
        await _fixture.ResetDatabaseAsync();
        var (events, bookings, unitOfWork, context) = _fixture.CreateRepositories();
        await using (context)
        {
            var user = await AddUserAsync(context, unitOfWork);
            var eventItem = CreateEvent();
            await events.AddAsync(eventItem);
            await unitOfWork.SaveChangesAsync();

            var booking = Booking.CreatePending(eventItem.Id, user.Id);
            await bookings.AddAsync(booking);
            await unitOfWork.SaveChangesAsync();

            var loaded = await bookings.GetByIdAsync(booking.Id);

            Assert.NotNull(loaded);
            Assert.Equal(booking.Id, loaded.Id);
            Assert.Equal(eventItem.Id, loaded.EventId);
            Assert.Equal(user.Id, loaded.UserId);
            Assert.Equal(BookingStatus.Pending, loaded.Status);
        }
    }

    [Fact]
    public async Task AddAsync_SavesAvailableSeatsChangeOnEvent()
    {
        await _fixture.ResetDatabaseAsync();
        var (events, bookings, unitOfWork, context) = _fixture.CreateRepositories();
        await using (context)
        {
            var user = await AddUserAsync(context, unitOfWork);
            var eventItem = CreateEvent(seats: 3);
            await events.AddAsync(eventItem);
            await unitOfWork.SaveChangesAsync();

            var tracked = await events.GetByIdAsync(eventItem.Id);
            Assert.True(tracked!.TryReserveSeats());

            var booking = Booking.CreatePending(tracked.Id, user.Id);
            await bookings.AddAsync(booking);
            await unitOfWork.SaveChangesAsync();

            var (events2, _, _, context2) = _fixture.CreateRepositories();
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
        var (_, bookings, _, context) = _fixture.CreateRepositories();
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
        var (events, bookings, unitOfWork, context) = _fixture.CreateRepositories();
        await using (context)
        {
            var user = await AddUserAsync(context, unitOfWork);
            var eventItem = CreateEvent(seats: 5);
            await events.AddAsync(eventItem);
            await unitOfWork.SaveChangesAsync();

            var pending1 = Booking.CreatePending(eventItem.Id, user.Id);
            var pending2 = Booking.CreatePending(eventItem.Id, user.Id);
            var confirmed = Booking.CreatePending(eventItem.Id, user.Id);
            await bookings.AddAsync(pending1);
            await bookings.AddAsync(pending2);
            await bookings.AddAsync(confirmed);
            await unitOfWork.SaveChangesAsync();

            confirmed.Confirm();
            await unitOfWork.SaveChangesAsync();

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
        var (events, bookings, unitOfWork, context) = _fixture.CreateRepositories();
        await using (context)
        {
            var user = await AddUserAsync(context, unitOfWork);
            var eventItem = CreateEvent();
            await events.AddAsync(eventItem);
            await unitOfWork.SaveChangesAsync();

            var booking = Booking.CreatePending(eventItem.Id, user.Id);
            await bookings.AddAsync(booking);
            await unitOfWork.SaveChangesAsync();

            booking.Confirm();
            await unitOfWork.SaveChangesAsync();

            var loaded = await bookings.GetByIdAsync(booking.Id);
            Assert.Equal(BookingStatus.Confirmed, loaded!.Status);
            Assert.NotNull(loaded.ProcessedAt);
        }
    }

    [Fact]
    public async Task SaveChangesAsync_Reject_AndReleaseSeats()
    {
        await _fixture.ResetDatabaseAsync();
        var (events, bookings, unitOfWork, context) = _fixture.CreateRepositories();
        await using (context)
        {
            var user = await AddUserAsync(context, unitOfWork);
            var eventItem = CreateEvent(seats: 1);
            await events.AddAsync(eventItem);
            await unitOfWork.SaveChangesAsync();

            var tracked = await events.GetByIdAsync(eventItem.Id);
            tracked!.TryReserveSeats();
            var booking = Booking.CreatePending(tracked.Id, user.Id);
            await bookings.AddAsync(booking);
            await unitOfWork.SaveChangesAsync();

            booking.Reject();
            tracked.ReleaseSeats();
            await unitOfWork.SaveChangesAsync();

            var loadedBooking = await bookings.GetByIdAsync(booking.Id);
            var loadedEvent = await events.GetByIdAsync(eventItem.Id);

            Assert.Equal(BookingStatus.Rejected, loadedBooking!.Status);
            Assert.Equal(1, loadedEvent!.AvailableSeats);
        }
    }

    [Fact]
    public async Task CountActiveByUserAsync_CountsPendingAndConfirmedOnly()
    {
        await _fixture.ResetDatabaseAsync();
        var (events, bookings, unitOfWork, context) = _fixture.CreateRepositories();
        await using (context)
        {
            var user = await AddUserAsync(context, unitOfWork);
            var other = await AddUserAsync(context, unitOfWork);
            var eventItem = CreateEvent(seats: 10);
            await events.AddAsync(eventItem);
            await unitOfWork.SaveChangesAsync();

            var pending = Booking.CreatePending(eventItem.Id, user.Id);
            var confirmed = Booking.CreatePending(eventItem.Id, user.Id);
            var rejected = Booking.CreatePending(eventItem.Id, user.Id);
            var otherUserBooking = Booking.CreatePending(eventItem.Id, other.Id);
            await bookings.AddAsync(pending);
            await bookings.AddAsync(confirmed);
            await bookings.AddAsync(rejected);
            await bookings.AddAsync(otherUserBooking);
            await unitOfWork.SaveChangesAsync();

            confirmed.Confirm();
            rejected.Reject();
            await unitOfWork.SaveChangesAsync();

            Assert.Equal(2, await bookings.CountActiveByUserAsync(user.Id));
            Assert.Equal(1, await bookings.CountActiveByUserAsync(other.Id));
        }
    }
}
