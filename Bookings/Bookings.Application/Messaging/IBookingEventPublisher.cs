using Shared.Contracts;

namespace Bookings.Application.Messaging;

/// <summary>
/// Публикация BookingLifecycleEvent в Kafka (из outbox)
/// </summary>
public interface IBookingEventPublisher
{
    /// <summary>
    /// Отправляет сообщение в топик booking-events
    /// </summary>
    Task PublishAsync(BookingLifecycleEvent message, CancellationToken cancellationToken = default);
}
