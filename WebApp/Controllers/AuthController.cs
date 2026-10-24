using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Application.Models;
using WebApp.Application.Services;

namespace WebApp.Controllers;

/// <summary>
/// Регистрация и вход (выдача JWT)
/// </summary>
[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    /// <summary>
    /// Создаёт контроллер аутентификации
    /// </summary>
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Зарегистрировать пользователя
    /// </summary>
    /// <param name="request">Логин, пароль и необязательная роль</param>
    /// <response code="204">Пользователь зарегистрирован</response>
    /// <response code="400">Ошибка валидации</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        await _authService.RegisterAsync(request);
        return NoContent();
    }

    /// <summary>
    /// Войти и получить JWT-токен
    /// </summary>
    /// <param name="request">Логин и пароль</param>
    /// <response code="200">Токен выдан</response>
    /// <response code="400">Неверные учётные данные</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        return Ok(await _authService.LoginAsync(request));
    }
}
