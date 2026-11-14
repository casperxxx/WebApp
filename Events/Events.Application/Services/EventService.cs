using Events.Application.Caching;
using Events.Application.Models;
using Events.Application.Repositories;
using Events.Domain.Entities;
using Events.Domain.Exceptions;

namespace Events.Application.Services;

/// <summary>
/// Сервис для работы с событиями (Cache-Aside для GetById и Top)
/// </summary>
internal class EventService : IEventService
{
    private const int TopCount = 10;

    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly RedisCacheOptions _cacheOptions;

    /// <summary>
    /// Создаёт сервис и получает зависимости через DI
    /// </summary>
    public EventService(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ICacheService cache,
        RedisCacheOptions cacheOptions)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _cacheOptions = cacheOptions;
    }

    /// <summary>
    /// Возвращает события с фильтрацией и пагинацией
    /// </summary>
    public async Task<PaginatedResultDTO<EventResponse>> GetEventsAsync(
        string? title,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize)
    {
        if (page < 1)
        {
            throw new DomainValidationException("page должен быть >= 1");
        }

        if (pageSize < 1)
        {
            throw new DomainValidationException("pageSize должен быть >= 1");
        }

        if (pageSize > 100)
        {
            throw new DomainValidationException("pageSize должен быть <= 100");
        }

        var skip = (page - 1) * pageSize;
        // данные берём из репозитория, валидацию page/pageSize оставляем в сервисе
        var (items, totalCount) = await _eventRepository.GetEventsAsync(title, from, to, skip, pageSize);

        return new PaginatedResultDTO<EventResponse>
        {
            TotalCount = totalCount,
            Items = items.Select(EventResponse.From).ToList(),
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Находит событие по Id (Cache-Aside: Redis → БД → Redis)
    /// </summary>
    public async Task<EventResponse> GetEventAsync(Guid id)
    {
        var cacheKey = CacheKeys.Event(id);
        var cached = await _cache.GetAsync<EventResponse>(cacheKey);
        if (cached is not null)
        {
            return cached;
        }

        var eventItem = await _eventRepository.GetByIdAsync(id);
        if (eventItem is null)
        {
            throw new NotFoundException($"Событие с id {id} не найдено");
        }

        var response = EventResponse.From(eventItem);
        await _cache.SetAsync(cacheKey, response, _cacheOptions.EventTtl);
        return response;
    }

    /// <summary>
    /// Топ-10 по доле проданных мест (Cache-Aside)
    /// </summary>
    public async Task<IReadOnlyList<EventResponse>> GetTopEventsAsync()
    {
        var cached = await _cache.GetAsync<List<EventResponse>>(CacheKeys.Top10);
        if (cached is not null)
        {
            return cached;
        }

        var items = await _eventRepository.GetTopBySoldRatioAsync(TopCount);
        var response = items.Select(EventResponse.From).ToList();
        await _cache.SetAsync(CacheKeys.Top10, response, _cacheOptions.TopTtl);
        return response;
    }

    /// <summary>
    /// Создаёт событие через Event.Create
    /// </summary>
    public async Task<EventResponse> CreateEventAsync(EventDTO request)
    {
        var totalSeats = request.TotalSeats ?? 0;
        var eventItem = Event.Create(
            request.Title,
            request.Description,
            request.StartAt,
            request.EndAt,
            totalSeats);

        await _eventRepository.AddAsync(eventItem);
        await _unitOfWork.SaveChangesAsync();

        return EventResponse.From(eventItem);
    }

    /// <summary>
    /// Обновляет данные события по Id
    /// </summary>
    public async Task<EventResponse> UpdateEventAsync(Guid id, EventDTO request)
    {
        var existing = await _eventRepository.GetByIdAsync(id);
        if (existing is null)
        {
            throw new NotFoundException($"Событие с id {id} не найдено");
        }

        existing.Update(
            request.Title,
            request.Description,
            request.StartAt,
            request.EndAt,
            request.TotalSeats ?? 0);

        await _unitOfWork.SaveChangesAsync();

        return EventResponse.From(existing);
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

        _eventRepository.Delete(eventItem);
        await _unitOfWork.SaveChangesAsync();
    }
}
