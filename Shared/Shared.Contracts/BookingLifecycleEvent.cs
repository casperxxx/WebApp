namespace Shared.Contracts;

/// <summary>
/// Сообщение жизненного цикла брони в одном топике (порядок по ключу EventId)
/// </summary>
public sealed record BookingLifecycleEvent(
    BookingEventType Type,
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    int Seats,
    DateTime OccurredAtUtc,
    string? Reason = null);
