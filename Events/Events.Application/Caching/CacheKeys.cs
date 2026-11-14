namespace Events.Application.Caching;

/// <summary>
/// Имена ключей Redis — в одном месте, чтобы не разбрасывать строки по коду
/// </summary>
public static class CacheKeys
{
    /// <summary>
    /// Кеш одного события: event:{id}
    /// </summary>
    public static string Event(Guid id) => $"event:{id}";

    /// <summary>
    /// Кеш топ-10 популярных событий
    /// </summary>
    public const string Top10 = "events:top10";
}
