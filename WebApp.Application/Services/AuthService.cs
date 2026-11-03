using WebApp.Application.Models;
using WebApp.Application.Repositories;
using WebApp.Application.Security;
using WebApp.Domain.Entities;
using WebApp.Domain.Exceptions;

namespace WebApp.Application.Services;

/// <summary>
/// Регистрация пользователей и выдача JWT при входе
/// </summary>
internal class AuthService : IAuthService
{
    private const string InvalidCredentialsMessage = "Неверный логин или пароль";

    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    /// <summary>
    /// Сохраняет пользователя с хешем пароля
    /// </summary>
    public async Task RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new DomainValidationException("Пароль не может быть пустым");
        }

        var login = request.Login.Trim();
        if (await _userRepository.ExistsByLoginAsync(login))
        {
            throw new DomainValidationException($"Логин '{login}' уже занят");
        }

        var role = request.Role ?? UserRole.User;
        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = User.Create(login, passwordHash, role);

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// Проверяет учётные данные и возвращает токен
    /// </summary>
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var login = request.Login.Trim();
        var user = await _userRepository.GetByLoginAsync(login);

        // одно сообщение — чтобы не подсказывать, существует ли логин
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new DomainValidationException(InvalidCredentialsMessage);
        }

        var token = _jwtTokenService.CreateToken(user.Id, user.Login, user.Role);
        return new LoginResponse { Token = token };
    }
}
