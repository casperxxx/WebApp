using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Events.Domain.Entities;

namespace Events.Infrastructure.DataAccess.Configurations;

/// <summary>
/// Fluent-конфигурация ProcessedCancellation
/// </summary>
internal sealed class ProcessedCancellationConfiguration : IEntityTypeConfiguration<ProcessedCancellation>
{
    public void Configure(EntityTypeBuilder<ProcessedCancellation> builder)
    {
        builder.ToTable("processed_cancellations");

        builder.HasKey(p => p.BookingId);
        builder.Property(p => p.BookingId).ValueGeneratedNever();
        builder.Property(p => p.ProcessedAtUtc).IsRequired();
    }
}
