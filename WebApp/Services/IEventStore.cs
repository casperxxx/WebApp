using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Хранилище событий
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Список всех событий
    /// </summary>
    List<Event> Events { get; }
}
