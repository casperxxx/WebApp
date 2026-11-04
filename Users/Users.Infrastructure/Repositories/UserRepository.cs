using Microsoft.EntityFrameworkCore;
using Users.Application.Repositories;
using Users.Domain.Entities;
using Users.Infrastructure.DataAccess;

namespace Users.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория пользователей через UsersDbContext
/// </summary>
internal sealed class UserRepository : IUserRepository
{
    private readonly UsersDbContext _context;

    public UserRepository(UsersDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByLoginAsync(string login, CancellationToken cancellationToken = default)
    {
        var normalized = login.Trim();
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Login == normalized, cancellationToken);
    }

    public async Task<bool> ExistsByLoginAsync(string login, CancellationToken cancellationToken = default)
    {
        var normalized = login.Trim();
        return await _context.Users.AnyAsync(u => u.Login == normalized, cancellationToken);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(user);
        return Task.CompletedTask;
    }
}
