using WebApp.Exceptions;
using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Сервис для работы с событиями
/// </summary>
public class EventService : IEventService
{
    private readonly IEventStore _store;

    /// <summary>
    /// Создаёт сервис и получает хранилище через DI
    /// </summary>
    /// <param name="store">Хранилище событий</param>
    public EventService(IEventStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Возвращает события с фильтрацией и пагинацией
    /// </summary>
    /// <param name="title">Поиск по названию</param>
    /// <param name="from">События, которые начинаются не раньше указанной даты</param>
    /// <param name="to">События, которые заканчиваются не позже указанной даты</param>
    /// <param name="page">Номер страницы</param>
    /// <param name="pageSize">Количество элементов на странице</param>
    /// <returns>Результат с пагинацией</returns>
    public PaginatedResultDTO<Event> GetEvents(string? title, DateTime? from, DateTime? to, int page, int pageSize)
    {
        if (page < 1)
        {
            throw new ArgumentException("page должен быть >= 1");
        }

        if (pageSize < 1)
        {
            throw new ArgumentException("pageSize должен быть >= 1");
        }

        if (pageSize > 100)
        {
            throw new ArgumentException("pageSize должен быть <= 100");
        }

        var query = _store.Events.AsEnumerable();

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

        var filtered = query.ToList();
        var totalCount = filtered.Count;
        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedResultDTO<Event>
        {
            TotalCount = totalCount,
            Items = items,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Находит событие по Id
    /// </summary>
    /// <param name="id">Id события</param>
    /// <returns>Событие с таким Id</returns>
    public Event GetEvent(Guid id)
    {
        var eventItem = _store.Events.FirstOrDefault(e => e.Id == id);
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
        ValidateDates(eventItem);

        eventItem.Id = Guid.NewGuid();
        _store.Events.Add(eventItem);
    }

    /// <summary>
    /// Создаёт событие через Event.Create
    /// </summary>
    public Task<Event> CreateEventAsync(EventDTO request)
    {
        var totalSeats = request.TotalSeats ?? 0;
        var eventItem = Event.Create(
            request.Title,
            request.Description,
            request.StartAt,
            request.EndAt,
            totalSeats);

        ValidateDates(eventItem);
        _store.Events.Add(eventItem);

        return Task.FromResult(eventItem);
    }

    /// <summary>
    /// Обновляет данные события по Id
    /// </summary>
    /// <param name="id">Id события</param>
    /// <param name="eventItem">Новые данные</param>
    public void UpdateEvent(Guid id, Event eventItem)
    {
        var index = _store.Events.FindIndex(e => e.Id == id);
        if (index == -1)
        {
            throw new NotFoundException($"Событие с id {id} не найдено");
        }

        ValidateDates(eventItem);

        var existing = _store.Events[index];
        var reservedSeats = existing.TotalSeats - existing.AvailableSeats;
        var totalSeats = eventItem.TotalSeats > 0 ? eventItem.TotalSeats : existing.TotalSeats;

        existing.Title = eventItem.Title;
        existing.Description = eventItem.Description;
        existing.StartAt = eventItem.StartAt;
        existing.EndAt = eventItem.EndAt;
        existing.TotalSeats = totalSeats;
        // не затираем уже занятые места
        existing.AvailableSeats = Math.Max(0, totalSeats - reservedSeats);
    }

    /// <summary>
    /// Удаляет событие из списка по Id
    /// </summary>
    /// <param name="id">Id события</param>
    public void DeleteEvent(Guid id)
    {
        var eventItem = _store.Events.FirstOrDefault(e => e.Id == id);
        if (eventItem is null)
        {
            throw new NotFoundException($"Событие с id {id} не найдено");
        }

        _store.Events.Remove(eventItem);
    }

    private static void ValidateDates(Event eventItem)
    {
        if (eventItem.EndAt <= eventItem.StartAt)
        {
            throw new ArgumentException("Дата и время завершения события должно быть позже времени начала.");
        }
    }
}
