namespace Events.Domain.Entities;

/// <summary>
/// Уже обработанный BookingId — чтобы повтор Kafka не списывал места снова
/// </summary>
public class ProcessedBooking
{
    private ProcessedBooking()
    {
    }

    /// <summary>
    /// Id брони из сообщения BookingConfirmed
    /// </summary>
    public Guid BookingId { get; private set; }

    /// <summary>
    /// Когда обработали сообщение
    /// </summary>
    public DateTime ProcessedAtUtc { get; private set; }

    /// <summary>
    /// Фиксирует обработку брони
    /// </summary>
    public static ProcessedBooking Create(Guid bookingId)
    {
        return new ProcessedBooking
        {
            BookingId = bookingId,
            ProcessedAtUtc = DateTime.UtcNow
        };
    }
}
