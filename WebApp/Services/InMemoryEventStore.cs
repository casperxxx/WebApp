using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Хранилище событий в памяти
/// </summary>
public class InMemoryEventStore : IEventStore
{
    /// <summary>
    /// Список всех событий
    /// </summary>
    public List<Event> Events { get; } = [];
}
