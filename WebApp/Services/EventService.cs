using WebApp.Exceptions;
using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Сервис для работы с событиями
/// </summary>
public class EventService : IEventService
{
    /// <summary>
    /// Список всех событий
    /// </summary>
    public static List<Event> Events { get; set; } = [];

    /// <summary>
    /// Возвращает события с фильтрацией
    /// </summary>
    public List<Event> GetEvents(string? title, DateTime? from, DateTime? to)
    {
        var query = Events.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(title))
        {
            query = query.Where(e => e.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
        }

        if (from.HasValue)
        {
            query = query.Where(e => e.StartAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(e => e.EndAt <= to.Value);
        }

        return query.ToList();
    }

    /// <summary>
    /// Находит событие по Id
    /// </summary>
    /// <param name="id">Id события</param>
    /// <returns>Событие с таким Id</returns>
    public Event GetEvent(Guid id)
    {
        var eventItem = Events.FirstOrDefault(e => e.Id == id);
        if (eventItem is null)
        {
            throw new NotFoundException($"Событие с id {id} не найдено");
        }

        return eventItem;
    }

    /// <summary>
    /// Добавляет событие в список
    /// </summary>
    /// <param name="eventItem">Событие для добавления</param>
    public void AddEvent(Event eventItem)
    {
        eventItem.Id = Guid.NewGuid();
        Events.Add(eventItem);
    }

    /// <summary>
    /// Обновляет данные события по Id
    /// </summary>
    /// <param name="id">Id события</param>
    /// <param name="eventItem">Новые данные</param>
    public void UpdateEvent(Guid id, Event eventItem)
    {
        var index = Events.FindIndex(e => e.Id == id);
        if (index == -1)
        {
            throw new NotFoundException($"Событие с id {id} не найдено");
        }

        eventItem.Id = id;
        Events[index] = eventItem;
    }

    /// <summary>
    /// Удаляет событие из списка по Id
    /// </summary>
    /// <param name="id">Id события</param>
    public void DeleteEvent(Guid id)
    {
        var eventItem = Events.FirstOrDefault(e => e.Id == id);
        if (eventItem is null)
        {
            throw new NotFoundException($"Событие с id {id} не найдено");
        }

        Events.Remove(eventItem);
    }
}
