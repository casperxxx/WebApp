namespace WebApp.Application.Models;

/// <summary>
/// Ответ с JWT-токеном после успешного входа
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT-токен
    /// </summary>
    public string Token { get; set; } = string.Empty;
}
