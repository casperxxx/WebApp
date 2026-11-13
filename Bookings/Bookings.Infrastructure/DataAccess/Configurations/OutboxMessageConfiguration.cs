using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Bookings.Domain.Entities;

namespace Bookings.Infrastructure.DataAccess.Configurations;

/// <summary>
/// Fluent-конфигурация OutboxMessage
/// </summary>
internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.EventType)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(o => o.PayloadJson).IsRequired();
        builder.Property(o => o.CreatedAtUtc).IsRequired();
        builder.Property(o => o.PublishedAtUtc);

        builder.HasIndex(o => o.PublishedAtUtc);
        builder.HasIndex(o => o.CreatedAtUtc);
    }
}
