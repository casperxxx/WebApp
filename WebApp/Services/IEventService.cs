using WebApp.Models;

namespace WebApp.Services;

public interface IEventService
{
    List<Event> GetEvents();
    Event GetEvent(Guid id);
    void AddEvent(Event eventItem);
    void UpdateEvent(Guid id, Event eventItem);
    void DeleteEvent(Guid id);
}
