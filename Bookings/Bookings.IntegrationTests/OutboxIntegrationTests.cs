using Microsoft.EntityFrameworkCore;
using Bookings.Application.Services;
using Bookings.Domain.Entities;
using Bookings.Infrastructure.DataAccess;
using Bookings.Infrastructure.Repositories;
using Testcontainers.PostgreSql;

namespace Bookings.IntegrationTests;

public class OutboxIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("bookings_it")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    private BookingsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BookingsDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        var context = new BookingsDbContext(options);
        context.Database.Migrate();
        return context;
    }

    [Fact]
    public async Task Confirm_AndOutbox_SavedInOneTransaction()
    {
        await using var context = CreateContext();
        var bookings = new BookingRepository(context);
        var outbox = new OutboxRepository(context);
        var uow = new Bookings.Infrastructure.DataAccess.UnitOfWork(context);

        var booking = Booking.CreatePending(Guid.NewGuid(), Guid.NewGuid());
        await bookings.AddAsync(booking);
        await uow.SaveChangesAsync();

        booking.Confirm();
        await outbox.AddAsync(OutboxHelper.CreateConfirmed(booking, DateTime.UtcNow));
        await uow.SaveChangesAsync();

        var loaded = await bookings.GetByIdAsync(booking.Id);
        var pending = await outbox.GetPendingAsync(10);

        Assert.Equal(BookingStatus.Confirmed, loaded!.Status);
        Assert.Equal(Booking.DefaultSeats, loaded.Seats);
        Assert.Single(pending);
        Assert.Equal(nameof(Shared.Contracts.BookingEventType.Confirmed), pending[0].EventType);
    }

    [Fact]
    public async Task CancelConfirmed_WritesCancelledOutbox()
    {
        await using var context = CreateContext();
        var bookings = new BookingRepository(context);
        var outbox = new OutboxRepository(context);
        var uow = new Bookings.Infrastructure.DataAccess.UnitOfWork(context);

        var booking = Booking.CreatePending(Guid.NewGuid(), Guid.NewGuid());
        await bookings.AddAsync(booking);
        await uow.SaveChangesAsync();

        booking.Confirm();
        await uow.SaveChangesAsync();

        booking.Cancel();
        await outbox.AddAsync(OutboxHelper.CreateCancelled(booking, DateTime.UtcNow));
        await uow.SaveChangesAsync();

        var pending = await outbox.GetPendingAsync(10);
        Assert.Single(pending);
        Assert.Equal(nameof(Shared.Contracts.BookingEventType.Cancelled), pending[0].EventType);
    }
}
