using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WebApp.Application.Security;
using WebApp.Domain.Entities;

namespace WebApp.Infrastructure.Security;

/// <summary>
/// Генерация JWT по параметрам из конфигурации (секция Jwt)
/// </summary>
internal sealed class JwtTokenService : IJwtTokenService
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;

    /// <summary>
    /// Читает Secret, Issuer, Audience и ExpirationMinutes из appsettings
    /// </summary>
    public JwtTokenService(IConfiguration configuration)
    {
        _secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("В конфигурации не задан Jwt:Secret");
        _issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("В конфигурации не задан Jwt:Issuer");
        _audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("В конфигурации не задан Jwt:Audience");

        var expiration = configuration["Jwt:ExpirationMinutes"];
        if (!int.TryParse(expiration, out _expirationMinutes) || _expirationMinutes <= 0)
        {
            throw new InvalidOperationException("В конфигурации не задан корректный Jwt:ExpirationMinutes");
        }
    }

    /// <inheritdoc />
    public string CreateToken(Guid userId, string login, UserRole role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, login),
            new Claim(ClaimTypes.Role, role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
