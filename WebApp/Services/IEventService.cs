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
    Task<PaginatedResultDTO<Event>> GetEventsAsync(
        string? title,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize);

    /// <summary>
    /// Получить событие по Id
    /// </summary>
    /// <param name="id">Id события</param>
    /// <returns>Найденное событие</returns>
    Task<Event> GetEventAsync(Guid id);

    /// <summary>
    /// Создать событие из DTO
    /// </summary>
    /// <param name="request">Данные для создания события</param>
    /// <returns>Созданное событие</returns>
    Task<Event> CreateEventAsync(EventDTO request);

    /// <summary>
    /// Изменить событие по id
    /// </summary>
    /// <param name="id">Id события</param>
    /// <param name="eventItem">Новые данные события</param>
    /// <returns>Обновлённое событие</returns>
    Task<Event> UpdateEventAsync(Guid id, Event eventItem);

    /// <summary>
    /// Удалить событие по id
    /// </summary>
    /// <param name="id">Id события</param>
    Task DeleteEventAsync(Guid id);
}
