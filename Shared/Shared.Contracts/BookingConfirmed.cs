namespace Shared.Contracts;

/// <summary>
/// Событие: бронь подтверждена (Bookings → Events через Kafka)
/// </summary>
public sealed record BookingConfirmed(
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    int Seats,
    DateTime ConfirmedAtUtc);
