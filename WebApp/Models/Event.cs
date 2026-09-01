using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebApp.Models;

/// <summary>
/// Модель события
/// </summary>
public class Event
{
    private Event()
    {
        Title = null!;
        Bookings = [];
    }

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

    /// <summary>
    /// Всего мест
    /// </summary>
    public int TotalSeats { get; set; }

    /// <summary>
    /// Сколько мест свободно
    /// </summary>
    public int AvailableSeats { get; set; }

    /// <summary>
    /// Брони на это событие
    /// </summary>
    [JsonIgnore]
    public ICollection<Booking> Bookings { get; private set; }

    /// <summary>
    /// Данные для обновления события из DTO
    /// </summary>
    public static Event FromUpdate(EventDTO request)
    {
        return new Event
        {
            Title = request.Title,
            Description = request.Description,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            TotalSeats = request.TotalSeats ?? 0
        };
    }

    /// <summary>
    /// Создаёт событие, AvailableSeats = TotalSeats
    /// </summary>
    public static Event Create(
        string title,
        string? description,
        DateTime startAt,
        DateTime endAt,
        int totalSeats)
    {
        if (totalSeats <= 0)
        {
            throw new ValidationException("TotalSeats должно быть больше нуля");
        }

        return new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            StartAt = startAt,
            EndAt = endAt,
            TotalSeats = totalSeats,
            AvailableSeats = totalSeats
        };
    }

    /// <summary>
    /// Занять места, если хватает
    /// </summary>
    public bool TryReserveSeats(int count = 1)
    {
        if (AvailableSeats < count)
        {
            return false;
        }

        AvailableSeats -= count;
        return true;
    }

    /// <summary>
    /// Вернуть места обратно
    /// </summary>
    public void ReleaseSeats(int count = 1)
    {
        AvailableSeats += count;

        if (AvailableSeats > TotalSeats)
        {
            AvailableSeats = TotalSeats;
        }
    }
}
