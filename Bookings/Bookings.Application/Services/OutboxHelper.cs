using System.Text.Json;
using Bookings.Domain.Entities;
using Shared.Contracts;

namespace Bookings.Application.Services;

/// <summary>
/// Сборка записей outbox из доменной брони
/// </summary>
internal static class OutboxHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static OutboxMessage CreateConfirmed(Booking booking, DateTime occurredAtUtc)
    {
        var payload = new BookingLifecycleEvent(
            BookingEventType.Confirmed,
            booking.Id,
            booking.EventId,
            booking.UserId,
            booking.Seats,
            occurredAtUtc);

        return OutboxMessage.Create(
            BookingEventType.Confirmed.ToString(),
            JsonSerializer.Serialize(payload, JsonOptions));
    }

    public static OutboxMessage CreateCancelled(Booking booking, DateTime occurredAtUtc)
    {
        var payload = new BookingLifecycleEvent(
            BookingEventType.Cancelled,
            booking.Id,
            booking.EventId,
            booking.UserId,
            booking.Seats,
            occurredAtUtc);

        return OutboxMessage.Create(
            BookingEventType.Cancelled.ToString(),
            JsonSerializer.Serialize(payload, JsonOptions));
    }
}
