using Bookings.Application.Repositories;

namespace Bookings.Infrastructure.DataAccess;

/// <summary>
/// Единица работы поверх BookingsDbContext
/// </summary>
internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly BookingsDbContext _context;

    public UnitOfWork(BookingsDbContext context)
    {
        _context = context;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
