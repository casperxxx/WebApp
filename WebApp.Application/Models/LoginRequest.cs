using System.ComponentModel.DataAnnotations;

namespace WebApp.Application.Models;

/// <summary>
/// Запрос на вход по логину и паролю
/// </summary>
public class LoginRequest
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
}
