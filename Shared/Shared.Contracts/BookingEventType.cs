namespace Shared.Contracts;

/// <summary>
/// Тип сообщения в топике booking-events
/// </summary>
public enum BookingEventType
{
    /// <summary>Бронь подтверждена — Events списывает места</summary>
    Confirmed = 1,

    /// <summary>Бронь отменена — Events возвращает места</summary>
    Cancelled = 2,

    /// <summary>Events не смог списать места — Bookings отклоняет бронь</summary>
    Rejected = 3
}
