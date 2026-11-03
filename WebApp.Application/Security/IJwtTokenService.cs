using WebApp.Domain.Entities;

namespace WebApp.Application.Security;

/// <summary>
/// Генерация JWT-токена по данным пользователя
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Формирует подписанный JWT-токен
    /// </summary>
    /// <param name="userId">Id пользователя</param>
    /// <param name="login">Логин</param>
    /// <param name="role">Роль</param>
    string CreateToken(Guid userId, string login, UserRole role);
}
