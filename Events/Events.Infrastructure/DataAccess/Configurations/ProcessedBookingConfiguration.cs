using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Events.Domain.Entities;

namespace Events.Infrastructure.DataAccess.Configurations;

/// <summary>
/// Fluent-конфигурация ProcessedBooking
/// </summary>
internal sealed class ProcessedBookingConfiguration : IEntityTypeConfiguration<ProcessedBooking>
{
    public void Configure(EntityTypeBuilder<ProcessedBooking> builder)
    {
        builder.ToTable("processed_bookings");

        builder.HasKey(p => p.BookingId);
        builder.Property(p => p.BookingId).ValueGeneratedNever();
        builder.Property(p => p.ProcessedAtUtc).IsRequired();
    }
}
