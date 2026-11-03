using WebApp.Domain.Entities;

namespace WebApp.Application.Repositories;

/// <summary>
/// Репозиторий для работы с пользователями в БД
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Найти пользователя по Id
    /// </summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Найти пользователя по логину
    /// </summary>
    Task<User?> GetByLoginAsync(string login, CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверить, занят ли логин
    /// </summary>
    Task<bool> ExistsByLoginAsync(string login, CancellationToken cancellationToken = default);

    /// <summary>
    /// Добавить пользователя в контекст (без SaveChanges)
    /// </summary>
    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
