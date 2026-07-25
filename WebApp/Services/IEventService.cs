using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Интерфейс сервиса
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Получить события с фильтрацией и пагинацией
    /// </summary>
    /// <param name="title">Поиск по названию</param>
    /// <param name="from">События, которые начинаются не раньше указанной даты</param>
    /// <param name="to">События, которые заканчиваются не позже указанной даты</param>
    /// <param name="page">Номер страницы</param>
    /// <param name="pageSize">Количество элементов на странице</param>
    /// <returns>Результат с пагинацией</returns>
    PaginatedResultDTO<Event> GetEvents(string? title, DateTime? from, DateTime? to, int page, int pageSize);

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
