using WebApp.Domain.Exceptions;

namespace WebApp.Domain.Entities;

/// <summary>
/// Модель пользователя
/// </summary>
public class User
{
    private User()
    {
        Login = null!;
        PasswordHash = null!;
        Bookings = [];
    }

    /// <summary>
    /// Id пользователя
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Логин пользователя
    /// </summary>
    public string Login { get; private set; }

    /// <summary>
    /// Хеш пароля
    /// </summary>
    public string PasswordHash { get; private set; }

    /// <summary>
    /// Роль пользователя
    /// </summary>
    public UserRole Role { get; private set; }

    /// <summary>
    /// Брони этого пользователя
    /// </summary>
    public ICollection<Booking> Bookings { get; private set; }

    /// <summary>
    /// Создаёт пользователя с уже посчитанным хешем пароля
    /// </summary>
    public static User Create(string login, string passwordHash, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            throw new DomainValidationException("Логин не может быть пустым");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainValidationException("Хеш пароля не может быть пустым");
        }

        return new User
        {
            Id = Guid.NewGuid(),
            Login = login.Trim(),
            PasswordHash = passwordHash,
            Role = role
        };
    }
}
