using Bookings.Domain.Entities;

namespace Bookings.Application.Repositories;

/// <summary>
/// Очередь исходящих Kafka-сообщений
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Добавить сообщение (без SaveChanges)
    /// </summary>
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Неотправленные сообщения в порядке создания
    /// </summary>
    Task<List<OutboxMessage>> GetPendingAsync(int take, CancellationToken cancellationToken = default);
}
