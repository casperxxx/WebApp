using WebApp.Domain.Entities;

namespace WebApp.Application.Models;

/// <summary>
/// Ответ API с данными брони (без навигаций EF)
/// </summary>
public class BookingResponse
{
    /// <summary>
    /// Id брони
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Id события
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Id пользователя, создавшего бронь
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Статус брони
    /// </summary>
    public BookingStatus Status { get; set; }

    /// <summary>
    /// Дата создания
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата обработки
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Маппинг из доменной сущности
    /// </summary>
    public static BookingResponse From(Booking booking) => new()
    {
        Id = booking.Id,
        EventId = booking.EventId,
        UserId = booking.UserId,
        Status = booking.Status,
        CreatedAt = booking.CreatedAt,
        ProcessedAt = booking.ProcessedAt
    };
}
