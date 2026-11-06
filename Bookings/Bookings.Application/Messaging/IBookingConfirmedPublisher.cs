using Shared.Contracts;

namespace Bookings.Application.Messaging;

/// <summary>
/// Публикация события BookingConfirmed (реализация — Kafka)
/// </summary>
public interface IBookingConfirmedPublisher
{
    /// <summary>
    /// Отправляет сообщение в топик после подтверждения брони
    /// </summary>
    Task PublishAsync(BookingConfirmed message, CancellationToken cancellationToken = default);
}
