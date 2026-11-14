namespace Events.Application.Caching;

/// <summary>
/// Параметры кеша из секции Redis в appsettings
/// </summary>
public sealed class RedisCacheOptions
{
    /// <summary>
    /// Секция конфигурации
    /// </summary>
    public const string SectionName = "Redis";

    /// <summary>
    /// Строка подключения к Redis (localhost:6379 / redis:6379)
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// TTL для event:{id} в секундах
    /// </summary>
    public int EventTtlSeconds { get; set; } = 300;

    /// <summary>
    /// TTL для events:top10 в секундах
    /// </summary>
    public int TopTtlSeconds { get; set; } = 60;

    /// <summary>
    /// TTL одного события
    /// </summary>
    public TimeSpan EventTtl => TimeSpan.FromSeconds(EventTtlSeconds);

    /// <summary>
    /// TTL топ-10
    /// </summary>
    public TimeSpan TopTtl => TimeSpan.FromSeconds(TopTtlSeconds);
}
