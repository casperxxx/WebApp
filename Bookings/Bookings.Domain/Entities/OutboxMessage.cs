namespace Bookings.Domain.Entities;

/// <summary>
/// Исходящее сообщение Kafka: пишется в одной транзакции с изменением брони
/// </summary>
public class OutboxMessage
{
    private OutboxMessage()
    {
        EventType = null!;
        PayloadJson = null!;
    }

    /// <summary>
    /// Id записи outbox
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Тип события: Confirmed / Cancelled
    /// </summary>
    public string EventType { get; private set; }

    /// <summary>
    /// JSON тела BookingLifecycleEvent
    /// </summary>
    public string PayloadJson { get; private set; }

    /// <summary>
    /// Когда запись создана
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Когда успешно отправили в Kafka (null = ещё не отправлено)
    /// </summary>
    public DateTime? PublishedAtUtc { get; private set; }

    /// <summary>
    /// Создаёт неотправленное сообщение
    /// </summary>
    public static OutboxMessage Create(string eventType, string payloadJson)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            PayloadJson = payloadJson,
            CreatedAtUtc = DateTime.UtcNow,
            PublishedAtUtc = null
        };
    }

    /// <summary>
    /// Пометить как отправленное
    /// </summary>
    public void MarkPublished()
    {
        PublishedAtUtc = DateTime.UtcNow;
    }
}
