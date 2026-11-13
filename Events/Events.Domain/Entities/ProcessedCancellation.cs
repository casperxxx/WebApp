namespace Events.Domain.Entities;

/// <summary>
/// Уже обработанная отмена брони — повтор Kafka не возвращает места дважды
/// </summary>
public class ProcessedCancellation
{
    private ProcessedCancellation()
    {
    }

    /// <summary>
    /// Id брони из сообщения BookingCancelled
    /// </summary>
    public Guid BookingId { get; private set; }

    /// <summary>
    /// Когда обработали сообщение
    /// </summary>
    public DateTime ProcessedAtUtc { get; private set; }

    /// <summary>
    /// Фиксирует обработку отмены
    /// </summary>
    public static ProcessedCancellation Create(Guid bookingId)
    {
        return new ProcessedCancellation
        {
            BookingId = bookingId,
            ProcessedAtUtc = DateTime.UtcNow
        };
    }
}
