using Microsoft.EntityFrameworkCore;
using WebApp.DataAccess;
using WebApp.Exceptions;
using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Сервис для работы с событиями
/// </summary>
internal class EventService : IEventService
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Создаёт сервис и получает контекст БД через DI
    /// </summary>
    /// <param name="context">Контекст базы данных</param>
    public EventService(AppDbContext context)
    {
        _context = context;
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

        var query = _context.Events.AsQueryable();

        if (!string.IsNullOrWhiteSpace(title))
        {
            var titleLower = title.ToLower();
            query = query.Where(e => e.Title.ToLower().Contains(titleLower));
        }

        if (from.HasValue)
        {
            query = query.Where(e => e.StartAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(e => e.EndAt <= to.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

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
        var eventItem = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
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
        _context.Events.Add(eventItem);
        await _context.SaveChangesAsync();

        return eventItem;
    }

    /// <summary>
    /// Обновляет данные события по Id
    /// </summary>
    public async Task<Event> UpdateEventAsync(Guid id, Event eventItem)
    {
        var existing = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
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

        await _context.SaveChangesAsync();

        return existing;
    }

    /// <summary>
    /// Удаляет событие из списка по Id
    /// </summary>
    public async Task DeleteEventAsync(Guid id)
    {
        var eventItem = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (eventItem is null)
        {
            throw new NotFoundException($"Событие с id {id} не найдено");
        }

        _context.Events.Remove(eventItem);
        await _context.SaveChangesAsync();
    }

    private static void ValidateDates(Event eventItem)
    {
        if (eventItem.EndAt <= eventItem.StartAt)
        {
            throw new ArgumentException("Дата и время завершения события должно быть позже времени начала.");
        }
    }
}
