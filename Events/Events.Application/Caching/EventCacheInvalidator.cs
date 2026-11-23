namespace Events.Application.Caching;

/// <summary>
/// Инвалидация event:{id} через ICacheService + IEventCacheGate
/// </summary>
public sealed class EventCacheInvalidator : IEventCacheInvalidator
{
    private readonly ICacheService _cache;
    private readonly IEventCacheGate _gate;

    /// <summary>
    /// Создаёт инвалидатор
    /// </summary>
    public EventCacheInvalidator(ICacheService cache, IEventCacheGate gate)
    {
        _cache = cache;
        _gate = gate;
    }

    /// <inheritdoc />
    public Task InvalidateEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var key = CacheKeys.Event(eventId);
        return _gate.RunAsync(key, () => _cache.RemoveAsync(key, cancellationToken), cancellationToken);
    }
}