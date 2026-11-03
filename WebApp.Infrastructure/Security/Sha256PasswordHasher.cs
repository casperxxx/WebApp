using System.Security.Cryptography;
using System.Text;
using WebApp.Application.Security;

namespace WebApp.Infrastructure.Security;

/// <summary>
/// Хеширование пароля через SHA-256 (hex-строка)
/// </summary>
internal sealed class Sha256PasswordHasher : IPasswordHasher
{
    /// <inheritdoc />
    public string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    /// <inheritdoc />
    public bool Verify(string password, string passwordHash)
    {
        var computed = Hash(password);
        return string.Equals(computed, passwordHash, StringComparison.OrdinalIgnoreCase);
    }
}
