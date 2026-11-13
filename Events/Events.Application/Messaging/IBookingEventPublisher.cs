using Shared.Contracts;

namespace Events.Application.Messaging;

/// <summary>
/// Публикация Rejected обратно в booking-events
/// </summary>
public interface IBookingEventPublisher
{
    /// <summary>
    /// Отправляет сообщение (обычно Rejected) в топик booking-events
    /// </summary>
    Task PublishAsync(BookingLifecycleEvent message, CancellationToken cancellationToken = default);
}
