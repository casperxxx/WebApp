using WebApp.Application.Models;

namespace WebApp.Application.Services;

/// <summary>
/// Сервис регистрации и входа пользователей
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Зарегистрировать нового пользователя
    /// </summary>
    /// <param name="request">Логин, пароль и необязательная роль</param>
    Task RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Войти по учётным данным и получить JWT
    /// </summary>
    /// <param name="request">Логин и пароль</param>
    Task<LoginResponse> LoginAsync(LoginRequest request);
}
