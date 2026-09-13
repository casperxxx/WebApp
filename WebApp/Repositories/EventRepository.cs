using Microsoft.EntityFrameworkCore;
using WebApp.DataAccess;
using WebApp.Models;

namespace WebApp.Repositories;

/// <summary>
/// Реализация репозитория событий через AppDbContext
/// </summary>
internal sealed class EventRepository : IEventRepository
{
    private readonly AppDbContext _context;

    public EventRepository(AppDbContext context)
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

    public async Task AddAsync(Event eventItem, CancellationToken cancellationToken = default)
    {
        _context.Events.Add(eventItem);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Event eventItem, CancellationToken cancellationToken = default)
    {
        // сущность уже отслеживается контекстом, просто сохраняем
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Event eventItem, CancellationToken cancellationToken = default)
    {
        _context.Events.Remove(eventItem);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
