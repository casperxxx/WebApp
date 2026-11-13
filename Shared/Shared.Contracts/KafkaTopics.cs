namespace Shared.Contracts;

/// <summary>
/// Имена топиков Kafka — одно место для издателя и подписчика
/// </summary>
public static class KafkaTopics
{
    /// <summary>
    /// Единый топик событий брони (Confirmed / Cancelled / Rejected).
    /// Ключ сообщения = EventId — порядок для одного события сохраняется.
    /// </summary>
    public const string BookingEvents = "booking-events";
}
