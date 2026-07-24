using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Интерфейс сервиса
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Получить события с фильтрацией
    /// </summary>
    /// <param name="title">Поиск по названию</param>
    /// <param name="from">События, которые начинаются не раньше указанной даты</param>
    /// <param name="to">События, которые заканчиваются не позже указанной даты</param>
    /// <returns>Список событий</returns>
    List<Event> GetEvents(string? title, DateTime? from, DateTime? to);

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
