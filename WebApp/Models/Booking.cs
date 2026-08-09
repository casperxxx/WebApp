namespace WebApp.Models;

/// <summary>
/// Модель бронирования
/// </summary>
public class Booking
{
    /// <summary>
    /// Id брони
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Id события, к которому относится бронь
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Текущий статус брони
    /// </summary>
    public BookingStatus Status { get; set; }

    /// <summary>
    /// Дата и время создания брони
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время обработки брони
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
}
