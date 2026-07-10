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
    /// Возвращает все события, если список пустой то ошибка
    /// </summary>
    /// <returns>Список событий</returns>
    public List<Event> GetEvents()
    {
        if (Events.Count == 0)
        {
            throw new InvalidOperationException("События не найдены");
        }

        return Events;
    }

    /// <summary>
    /// Находит событие по Id
    /// </summary>
    /// <param name="id">Id события</param>
    /// <returns>Событие с таким Id</returns>
    public Event GetEvent(Guid id)
    {
        return Events.First(e => e.Id == id);
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
        eventItem.Id = id;
        Events[index] = eventItem;
    }

    /// <summary>
    /// Удаляет событие из списка по Id
    /// </summary>
    /// <param name="id">Id события</param>
    public void DeleteEvent(Guid id)
    {
        var eventItem = Events.First(e => e.Id == id);
        Events.Remove(eventItem);
    }
}
