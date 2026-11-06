using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Bookings.Domain.Entities;

namespace Bookings.Infrastructure.DataAccess.Configurations;

/// <summary>
/// Fluent-конфигурация сущности Booking
/// </summary>
internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.EventId).IsRequired();
        builder.Property(b => b.UserId).IsRequired();

        // индексы по внешним Id (сами сущности Event/User живут в других сервисах)
        builder.HasIndex(b => b.EventId);
        builder.HasIndex(b => b.UserId);

        builder.Property(b => b.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.ProcessedAt);
    }
}
