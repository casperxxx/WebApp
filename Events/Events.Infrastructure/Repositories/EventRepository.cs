using Microsoft.EntityFrameworkCore;
using Events.Application.Repositories;
using Events.Domain.Entities;
using Events.Infrastructure.DataAccess;

namespace Events.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория событий через EventsDbContext
/// </summary>
internal sealed class EventRepository : IEventRepository
{
    private readonly EventsDbContext _context;

    public EventRepository(EventsDbContext context)
    {
        _context = context;
    }

    public async Task<(List<Event> Items, int TotalCount)> GetEventsAsync(
        string? title,
        DateTime? from,
        DateTime? to,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Events.AsQueryable();

        // фильтр по названию (без учёта регистра)
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

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Event>> GetTopBySoldRatioAsync(int take, CancellationToken cancellationToken = default)
    {
        // популярность = доля проданных мест; TotalSeats=0 не делим
        return await _context.Events
            .OrderByDescending(e => e.TotalSeats == 0
                ? 0
                : (double)(e.TotalSeats - e.AvailableSeats) / e.TotalSeats)
            .ThenByDescending(e => e.TotalSeats - e.AvailableSeats)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Event eventItem, CancellationToken cancellationToken = default)
    {
        _context.Events.Add(eventItem);
        return Task.CompletedTask;
    }

    public void Delete(Event eventItem)
    {
        _context.Events.Remove(eventItem);
    }
}
