using Microsoft.EntityFrameworkCore;
using Events.Application.Repositories;
using Events.Domain.Entities;
using Events.Infrastructure.DataAccess;

namespace Events.Infrastructure.Repositories;

/// <summary>
/// Репозиторий обработанных отмен через EventsDbContext
/// </summary>
internal sealed class ProcessedCancellationRepository : IProcessedCancellationRepository
{
    private readonly EventsDbContext _context;

    public ProcessedCancellationRepository(EventsDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExistsAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        return _context.ProcessedCancellations.AnyAsync(p => p.BookingId == bookingId, cancellationToken);
    }

    public Task AddAsync(ProcessedCancellation processed, CancellationToken cancellationToken = default)
    {
        _context.ProcessedCancellations.Add(processed);
        return Task.CompletedTask;
    }
}
