using WebApp.Application.Models;

namespace WebApp.Application.Services;

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
    Task<PaginatedResultDTO<EventResponse>> GetEventsAsync(
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
    Task<EventResponse> GetEventAsync(Guid id);

    /// <summary>
    /// Создать событие из DTO
    /// </summary>
    /// <param name="request">Данные для создания события</param>
    /// <returns>Созданное событие</returns>
    Task<EventResponse> CreateEventAsync(EventDTO request);

    /// <summary>
    /// Изменить событие по id
    /// </summary>
    /// <param name="id">Id события</param>
    /// <param name="request">Новые данные события</param>
    /// <returns>Обновлённое событие</returns>
    Task<EventResponse> UpdateEventAsync(Guid id, EventDTO request);

    /// <summary>
    /// Удалить событие по id
    /// </summary>
    /// <param name="id">Id события</param>
    Task DeleteEventAsync(Guid id);
}
