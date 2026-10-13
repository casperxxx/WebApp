using WebApp.Application.Repositories;

namespace WebApp.Infrastructure.DataAccess;

/// <summary>
/// Единица работы поверх AppDbContext
/// </summary>
internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
