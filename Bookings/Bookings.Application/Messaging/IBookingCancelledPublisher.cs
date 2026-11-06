using Shared.Contracts;

namespace Bookings.Application.Messaging;

/// <summary>
/// Публикация события BookingCancelled (реализация — Kafka)
/// </summary>
public interface IBookingCancelledPublisher
{
    /// <summary>
    /// Отправляет сообщение в топик после отмены подтверждённой брони
    /// </summary>
    Task PublishAsync(BookingCancelled message, CancellationToken cancellationToken = default);
}
