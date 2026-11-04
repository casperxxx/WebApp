using Users.Application.Repositories;

namespace Users.Infrastructure.DataAccess;

/// <summary>
/// Единица работы поверх UsersDbContext
/// </summary>
internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly UsersDbContext _context;

    public UnitOfWork(UsersDbContext context)
    {
        _context = context;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
