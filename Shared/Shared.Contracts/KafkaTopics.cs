namespace Shared.Contracts;

/// <summary>
/// Имена топиков Kafka — одно место для издателя и подписчика
/// </summary>
public static class KafkaTopics
{
    /// <summary>
    /// Топик подтверждённых броней
    /// </summary>
    public const string BookingConfirmed = "booking-confirmed";
}
