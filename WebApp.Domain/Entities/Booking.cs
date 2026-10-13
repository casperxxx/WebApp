namespace WebApp.Domain.Entities;

/// <summary>
/// Модель бронирования
/// </summary>
public class Booking
{
    private Booking()
    {
    }

    /// <summary>
    /// Id брони
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Id события, к которому относится бронь
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// Событие, к которому относится бронь
    /// </summary>
    public Event? Event { get; private set; }

    /// <summary>
    /// Текущий статус брони
    /// </summary>
    public BookingStatus Status { get; private set; }

    /// <summary>
    /// Дата и время создания брони
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Дата и время обработки брони
    /// </summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>
    /// Создаёт бронь в статусе Pending
    /// </summary>
    public static Booking CreatePending(Guid eventId)
    {
        return new Booking
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = null
        };
    }

    /// <summary>
    /// Подтвердить бронь
    /// </summary>
    public void Confirm()
    {
        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Отклонить бронь
    /// </summary>
    public void Reject()
    {
        Status = BookingStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
    }
}
