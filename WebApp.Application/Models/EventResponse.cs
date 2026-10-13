using WebApp.Domain.Entities;

namespace WebApp.Application.Models;

/// <summary>
/// Ответ API с данными события (без навигаций EF)
/// </summary>
public class EventResponse
{
    /// <summary>
    /// Id события
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Название события
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Описание события
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Дата начала события
    /// </summary>
    public DateTime StartAt { get; set; }

    /// <summary>
    /// Дата завершения события
    /// </summary>
    public DateTime EndAt { get; set; }

    /// <summary>
    /// Всего мест
    /// </summary>
    public int TotalSeats { get; set; }

    /// <summary>
    /// Сколько мест свободно
    /// </summary>
    public int AvailableSeats { get; set; }

    /// <summary>
    /// Маппинг из доменной сущности
    /// </summary>
    public static EventResponse From(Event eventItem) => new()
    {
        Id = eventItem.Id,
        Title = eventItem.Title,
        Description = eventItem.Description,
        StartAt = eventItem.StartAt,
        EndAt = eventItem.EndAt,
        TotalSeats = eventItem.TotalSeats,
        AvailableSeats = eventItem.AvailableSeats
    };
}
