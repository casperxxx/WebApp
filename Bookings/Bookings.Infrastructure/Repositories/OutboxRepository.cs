using Microsoft.EntityFrameworkCore;
using Bookings.Application.Repositories;
using Bookings.Domain.Entities;
using Bookings.Infrastructure.DataAccess;

namespace Bookings.Infrastructure.Repositories;

/// <summary>
/// Outbox через BookingsDbContext
/// </summary>
internal sealed class OutboxRepository : IOutboxRepository
{
    private readonly BookingsDbContext _context;

    public OutboxRepository(BookingsDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        _context.OutboxMessages.Add(message);
        return Task.CompletedTask;
    }

    public Task<List<OutboxMessage>> GetPendingAsync(int take, CancellationToken cancellationToken = default)
    {
        return _context.OutboxMessages
            .Where(o => o.PublishedAtUtc == null)
            .OrderBy(o => o.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
