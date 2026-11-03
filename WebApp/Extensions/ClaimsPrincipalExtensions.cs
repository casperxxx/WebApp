using System.Security.Claims;
using WebApp.Domain.Entities;
using WebApp.Domain.Exceptions;

namespace WebApp.Extensions;

/// <summary>
/// Чтение данных текущего пользователя из JWT claims
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Id пользователя из ClaimTypes.NameIdentifier
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out var userId))
        {
            throw new DomainValidationException("В токене отсутствует идентификатор пользователя");
        }

        return userId;
    }

    /// <summary>
    /// Проверка роли Admin
    /// </summary>
    public static bool IsAdmin(this ClaimsPrincipal user) =>
        user.IsInRole(nameof(UserRole.Admin));
}
