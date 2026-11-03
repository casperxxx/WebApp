using WebApp.Domain.Exceptions;

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
    /// Id пользователя, создавшего бронь
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Пользователь, создавший бронь
    /// </summary>
    public User? User { get; private set; }

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
    public static Booking CreatePending(Guid eventId, Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainValidationException("UserId не может быть пустым");
        }

        return new Booking
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
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

    /// <summary>
    /// Отменить бронь (повторная отмена и отмена отклонённой — ошибка)
    /// </summary>
    public void Cancel()
    {
        if (Status == BookingStatus.Cancelled)
        {
            throw new DomainValidationException("Бронь уже отменена");
        }

        if (Status == BookingStatus.Rejected)
        {
            throw new DomainValidationException("Отклонённую бронь нельзя отменить");
        }

        Status = BookingStatus.Cancelled;
        ProcessedAt = DateTime.UtcNow;
    }
}
