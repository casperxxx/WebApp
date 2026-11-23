namespace Events.Application.Caching;

/// <summary>
/// Блокировка по ключу кеша: один запрос грузит из БД, остальные ждут (без stampede).
/// Тот же замок используется при инвалидации, чтобы не записать устаревшие данные после Remove.
/// </summary>
public interface IEventCacheGate
{
    /// <summary>
    /// Выполнить действие под семафором для ключа
    /// </summary>
    Task<T> RunAsync<T>(string key, Func<Task<T>> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Выполнить действие без результата под семафором для ключа
    /// </summary>
    Task RunAsync(string key, Func<Task> action, CancellationToken cancellationToken = default);
}
