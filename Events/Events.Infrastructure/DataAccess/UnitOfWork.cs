using Events.Application.Repositories;

namespace Events.Infrastructure.DataAccess;

/// <summary>
/// Единица работы поверх EventsDbContext
/// </summary>
internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly EventsDbContext _context;

    public UnitOfWork(EventsDbContext context)
    {
        _context = context;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
