using Microsoft.EntityFrameworkCore;
using Events.Domain.Entities;

namespace Events.Infrastructure.DataAccess;

/// <summary>
/// DbContext сервиса Events (events + processed_bookings + processed_cancellations)
/// </summary>
public sealed class EventsDbContext : DbContext
{
    public EventsDbContext(DbContextOptions<EventsDbContext> options) : base(options)
    {
    }

    public DbSet<Event> Events => Set<Event>();

    public DbSet<ProcessedBooking> ProcessedBookings => Set<ProcessedBooking>();

    public DbSet<ProcessedCancellation> ProcessedCancellations => Set<ProcessedCancellation>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventsDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // даты без зоны → UTC; с зоной → ToUniversalTime
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcNullableDateTimeConverter>();
    }
}
