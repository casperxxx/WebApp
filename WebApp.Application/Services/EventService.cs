using WebApp.Application.Models;
using WebApp.Application.Repositories;
using WebApp.Domain.Entities;
using WebApp.Domain.Exceptions;

namespace WebApp.Application.Services;

/// <summary>
/// Сервис для работы с событиями
/// </summary>
internal class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Создаёт сервис и получает репозиторий через DI
    /// </summary>
    public EventService(IEventRepository eventRepository, IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
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
    /// Находит событие по Id
    /// </summary>
    public async Task<EventResponse> GetEventAsync(Guid id)
    {
        var eventItem = await _eventRepository.GetByIdAsync(id);
        if (eventItem is null)
        {
            throw new NotFoundException($"Событие с id {id} не найдено");
        }

        return EventResponse.From(eventItem);
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
