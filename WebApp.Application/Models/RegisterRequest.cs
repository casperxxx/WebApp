using System.ComponentModel.DataAnnotations;
using WebApp.Domain.Entities;

namespace WebApp.Application.Models;

/// <summary>
/// Запрос на регистрацию пользователя
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// Логин
    /// </summary>
    [Required(ErrorMessage = "Требуется логин")]
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// Пароль
    /// </summary>
    [Required(ErrorMessage = "Требуется пароль")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Роль (по умолчанию User; для удобства тестов можно передать Admin)
    /// </summary>
    public UserRole? Role { get; set; }
}
