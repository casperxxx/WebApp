namespace Events.Application.Caching;

/// <summary>
/// Сброс кеша события: CRUD и Kafka consumer ходят сюда, а не в Redis напрямую
/// </summary>
public interface IEventCacheInvalidator
{
    /// <summary>
    /// Удалить ключ event:{id} под замком ключа
    /// </summary>
    Task InvalidateEventAsync(Guid eventId, CancellationToken cancellationToken = default);
}
