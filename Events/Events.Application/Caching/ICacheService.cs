namespace Events.Application.Caching;

/// <summary>
/// Абстракция кеша: Application не знает про Redis
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Получить значение по ключу (null — промах или ошибка кеша)
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Записать значение с TTL
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удалить ключ (инвалидация)
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
