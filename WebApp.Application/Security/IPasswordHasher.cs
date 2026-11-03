namespace WebApp.Application.Security;

/// <summary>
/// Хеширование паролей и проверка соответствия
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Считает хеш пароля
    /// </summary>
    /// <param name="password">Пароль в открытом виде</param>
    string Hash(string password);

    /// <summary>
    /// Проверяет, что пароль соответствует сохранённому хешу
    /// </summary>
    /// <param name="password">Пароль в открытом виде</param>
    /// <param name="passwordHash">Сохранённый хеш</param>
    bool Verify(string password, string passwordHash);
}
