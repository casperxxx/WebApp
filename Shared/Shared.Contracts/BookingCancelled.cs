namespace Shared.Contracts;

/// <summary>
/// Событие: бронь отменена (Bookings → Events через Kafka)
/// </summary>
public sealed record BookingCancelled(
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    int Seats,
    DateTime CancelledAtUtc);
