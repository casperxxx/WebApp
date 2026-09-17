using WebApp.Exceptions;
using WebApp.Models;
using WebApp.Repositories;

namespace WebApp.Services;

/// <summary>
/// Сервис для работы с событиями
/// </summary>
internal class EventService : IEventService
{
    // вместо AppDbContext работаем через репозиторий
    private readonly IEventRepository _eventRepository;

    /// <summary>
    /// Создаёт сервис и получает репозиторий через DI
    /// </summary>
    public EventService(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    /// <summary>
    /// Возвращает события с фильтрацией и пагинацией
    /// </summary>
    public async Task<PaginatedResultDTO<Event>> GetEventsAsync(
        string? title,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize)
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

        var skip = (page - 1) * pageSize;
        // данные берём из репозитория, валидацию page/pageSize оставляем в сервисе
        var (items, totalCount) = await _eventRepository.GetEventsAsync(title, from, to, skip, pageSize);

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
    public async Task<Event> GetEventAsync(Guid id)
    {
        var eventItem = await _eventRepository.GetByIdAsync(id);
        if (eventItem is null)
        {
            throw new NotFoundException($"Событие с id {id} не найдено");
        }

        return eventItem;
    }

    /// <summary>
    /// Создаёт событие через Event.Create
    /// </summary>
    public async Task<Event> CreateEventAsync(EventDTO request)
    {
        var totalSeats = request.TotalSeats ?? 0;
        var eventItem = Event.Create(
            request.Title,
            request.Description,
            request.StartAt,
            request.EndAt,
            totalSeats);

        ValidateDates(eventItem);
        await _eventRepository.AddAsync(eventItem);

        return eventItem;
    }

    /// <summary>
    /// Обновляет данные события по Id
    /// </summary>
    public async Task<Event> UpdateEventAsync(Guid id, Event eventItem)
    {
        var existing = await _eventRepository.GetByIdAsync(id);
        if (existing is null)
        {
            throw new NotFoundException($"Событие с id {id} не найдено");
        }

        ValidateDates(eventItem);

        var reservedSeats = existing.TotalSeats - existing.AvailableSeats;
        var totalSeats = eventItem.TotalSeats > 0 ? eventItem.TotalSeats : existing.TotalSeats;

        existing.Title = eventItem.Title;
        existing.Description = eventItem.Description;
        existing.StartAt = eventItem.StartAt;
        existing.EndAt = eventItem.EndAt;
        existing.TotalSeats = totalSeats;
        // не затираем уже занятые места
        existing.AvailableSeats = Math.Max(0, totalSeats - reservedSeats);

        await _eventRepository.UpdateAsync(existing);

        return existing;
    }

    /// <summary>
    /// Удаляет событие из списка по Id
    /// </summary>
    public async Task DeleteEventAsync(Guid id)
    {
        var eventItem = await _eventRepository.GetByIdAsync(id);
        if (eventItem is null)
        {
            throw new NotFoundException($"Событие с id {id} не найдено");
        }

        await _eventRepository.DeleteAsync(eventItem);
    }

    private static void ValidateDates(Event eventItem)
    {
        if (eventItem.EndAt <= eventItem.StartAt)
        {
            throw new ArgumentException("Дата и время завершения события должно быть позже времени начала.");
        }
    }
}
