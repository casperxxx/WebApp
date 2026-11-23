using System.Collections.Concurrent;

namespace Events.Application.Caching;

/// <summary>
/// In-process семафоры по ключу (один инстанс API). Singleton в DI.
/// </summary>
public sealed class EventCacheGate : IEventCacheGate
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    /// <inheritdoc />
    public async Task<T> RunAsync<T>(string key, Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        var gate = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await action();
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public Task RunAsync(string key, Func<Task> action, CancellationToken cancellationToken = default) =>
        RunAsync(key, async () =>
        {
            await action();
            return true;
        }, cancellationToken);
}
