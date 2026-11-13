using Bookings.Domain.Exceptions;

namespace Bookings.Domain.Entities;

/// <summary>
/// Модель бронирования
/// </summary>
public class Booking
{
    /// <summary>
    /// Сколько мест занимает одна бронь (пока всегда 1)
    /// </summary>
    public const int DefaultSeats = 1;

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
    /// Id пользователя, создавшего бронь
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Сколько мест занимает бронь
    /// </summary>
    public int Seats { get; private set; }

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
    public static Booking CreatePending(Guid eventId, Guid userId, int seats = DefaultSeats)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainValidationException("UserId не может быть пустым");
        }

        if (eventId == Guid.Empty)
        {
            throw new DomainValidationException("EventId не может быть пустым");
        }

        if (seats <= 0)
        {
            throw new DomainValidationException("Seats должно быть больше нуля");
        }

        return new Booking
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            Seats = seats,
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
        if (Status != BookingStatus.Pending)
        {
            throw new DomainValidationException("Подтвердить можно только Pending-бронь");
        }

        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Отклонить бронь (нет мест / событие не найдено и т.п.)
    /// </summary>
    public void Reject()
    {
        if (Status is BookingStatus.Cancelled or BookingStatus.Rejected)
        {
            throw new DomainValidationException("Бронь уже завершена и не может быть отклонена");
        }

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
