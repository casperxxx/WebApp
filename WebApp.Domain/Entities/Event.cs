using WebApp.Domain.Exceptions;

namespace WebApp.Domain.Entities;

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
    public Guid Id { get; private set; }

    /// <summary>
    /// Название события
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// Описание события
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Дата начала события
    /// </summary>
    public DateTime StartAt { get; private set; }

    /// <summary>
    /// Дата завершения события
    /// </summary>
    public DateTime EndAt { get; private set; }

    /// <summary>
    /// Всего мест
    /// </summary>
    public int TotalSeats { get; private set; }

    /// <summary>
    /// Сколько мест свободно
    /// </summary>
    public int AvailableSeats { get; private set; }

    /// <summary>
    /// Брони на это событие
    /// </summary>
    public ICollection<Booking> Bookings { get; private set; }

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
        ValidateDates(startAt, endAt);

        if (totalSeats <= 0)
        {
            throw new DomainValidationException("TotalSeats должно быть больше нуля");
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
    /// Обновляет данные события с проверкой занятых мест
    /// </summary>
    public void Update(
        string title,
        string? description,
        DateTime startAt,
        DateTime endAt,
        int totalSeats)
    {
        ValidateDates(startAt, endAt);

        if (totalSeats <= 0)
        {
            throw new DomainValidationException("TotalSeats должно быть больше нуля");
        }

        var reservedSeats = TotalSeats - AvailableSeats;
        if (totalSeats < reservedSeats)
        {
            throw new DomainValidationException(
                $"TotalSeats не может быть меньше числа занятых мест ({reservedSeats})");
        }

        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
        TotalSeats = totalSeats;
        AvailableSeats = totalSeats - reservedSeats;
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

    private static void ValidateDates(DateTime startAt, DateTime endAt)
    {
        if (endAt <= startAt)
        {
            throw new DomainValidationException(
                "Дата и время завершения события должно быть позже времени начала.");
        }
    }
}
