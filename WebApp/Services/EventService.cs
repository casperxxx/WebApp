using WebApp.Models;

namespace WebApp.Services;

public class EventService : IEventService
{
    // Коллекция для хранения событий
    public static List<Event> Events { get; set; } = [];

    // Метод получения всех событий
    public List<Event> GetEvents()
    {
        return Events;
    }

    // Метод получения события по id
    public Event GetEvent(Guid id)
    {
        return Events.First(e => e.Id == id);
    }

    // Метод добавления события
    public void AddEvent(Event eventItem)
    {
        eventItem.Id = Guid.NewGuid();
        Events.Add(eventItem);
    }

    // Метод изменения события по id
    public void UpdateEvent(Guid id, Event eventItem)
    {
        var index = Events.FindIndex(e => e.Id == id);
        eventItem.Id = id;
        Events[index] = eventItem;
    }

    // Метод удаления события по id
    public void DeleteEvent(Guid id)
    {
        var eventItem = Events.First(e => e.Id == id);
        Events.Remove(eventItem);
    }
}
