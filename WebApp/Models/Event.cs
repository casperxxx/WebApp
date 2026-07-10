namespace WebApp.Models;

/// <summary>
/// Модель события
/// </summary>
public class Event
{
    /// <summary>
    /// Id события
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Название события
    /// </summary>
    public required string Title { get; set; }

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
}
