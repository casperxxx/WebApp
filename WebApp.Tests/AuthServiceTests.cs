using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Application.Models;
using WebApp.Application.Repositories;
using WebApp.Application.Security;
using WebApp.Application.Services;
using WebApp.Domain.Entities;
using WebApp.Domain.Exceptions;
using WebApp.Infrastructure.DataAccess;
using WebApp.Infrastructure.Repositories;
using WebApp.Infrastructure.Security;

namespace WebApp.Tests;

/// <summary>
/// Юнит-тесты регистрации и входа
/// </summary>
public class AuthServiceTests : IDisposable
{
    private const string JwtSecret = "TestSecretKey_for_unit_tests_min32!!";
    private const string JwtIssuer = "WebApp";
    private const string JwtAudience = "WebApp";

    private readonly ServiceProvider _serviceProvider;

    public AuthServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = JwtSecret,
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:ExpirationMinutes"] = "60"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IPasswordHasher, Sha256PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose() => _serviceProvider.Dispose();

    [Fact]
    public async Task Register_ThenLogin_ReturnsToken()
    {
        using var scope = _serviceProvider.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        await auth.RegisterAsync(new RegisterRequest
        {
            Login = "student",
            Password = "secret",
            Role = UserRole.User
        });

        var result = await auth.LoginAsync(new LoginRequest
        {
            Login = "student",
            Password = "secret"
        });

        Assert.False(string.IsNullOrWhiteSpace(result.Token));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.Equal(JwtIssuer, jwt.Issuer);
        Assert.Contains(JwtAudience, jwt.Audiences);
        Assert.Equal("student", jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal(UserRole.User.ToString(), jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value);
        Assert.True(Guid.TryParse(
            jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value,
            out _));
    }

    [Fact]
    public async Task Register_DuplicateLogin_Throws()
    {
        using var scope = _serviceProvider.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        await auth.RegisterAsync(new RegisterRequest { Login = "dup", Password = "x" });

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            auth.RegisterAsync(new RegisterRequest { Login = "dup", Password = "y" }));
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsSameMessage()
    {
        using var scope = _serviceProvider.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        await auth.RegisterAsync(new RegisterRequest { Login = "u1", Password = "ok" });

        var ex = await Assert.ThrowsAsync<DomainValidationException>(() =>
            auth.LoginAsync(new LoginRequest { Login = "u1", Password = "bad" }));

        Assert.Equal("Неверный логин или пароль", ex.Message);
    }

    [Fact]
    public async Task Login_UnknownUser_ThrowsSameMessage()
    {
        using var scope = _serviceProvider.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var ex = await Assert.ThrowsAsync<DomainValidationException>(() =>
            auth.LoginAsync(new LoginRequest { Login = "missing", Password = "x" }));

        Assert.Equal("Неверный логин или пароль", ex.Message);
    }
}
