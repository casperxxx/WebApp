using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Интерфейс сервиса
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Получить все события из списка
    /// </summary>
    /// <returns>Список событий</returns>
    List<Event> GetEvents();

    /// <summary>
    /// Получить событие по Id
    /// </summary>
    /// <param name="id">Id события</param>
    /// <returns>Найденное событие</returns>
    Event GetEvent(Guid id);

    /// <summary>
    /// Добавить новое событие в список
    /// </summary>
    /// <param name="eventItem">Событие которое добавляем</param>
    void AddEvent(Event eventItem);

    /// <summary>
    /// Изменить событие по id
    /// </summary>
    /// <param name="id">Id события</param>
    /// <param name="eventItem">Новые данные события</param>
    void UpdateEvent(Guid id, Event eventItem);

    /// <summary>
    /// Удалить событие по id
    /// </summary>
    /// <param name="id">Id события</param>
    void DeleteEvent(Guid id);
}
