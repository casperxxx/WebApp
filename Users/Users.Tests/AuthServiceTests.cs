using Moq;
using Users.Application.Models;
using Users.Application.Repositories;
using Users.Application.Security;
using Users.Application.Services;
using Users.Domain.Entities;
using Users.Domain.Exceptions;

namespace Users.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenService> _jwt = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_users.Object, _uow.Object, _hasher.Object, _jwt.Object);
    }

    [Fact]
    public async Task Register_DuplicateLogin_Throws()
    {
        _users.Setup(r => r.ExistsByLoginAsync("alice", default)).ReturnsAsync(true);

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _sut.RegisterAsync(new RegisterRequest { Login = "alice", Password = "x" }));
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsSameMessage()
    {
        var user = User.Create("alice", "hash", UserRole.User);
        _users.Setup(r => r.GetByLoginAsync("alice", default)).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("bad", "hash")).Returns(false);

        var ex = await Assert.ThrowsAsync<DomainValidationException>(() =>
            _sut.LoginAsync(new LoginRequest { Login = "alice", Password = "bad" }));

        Assert.Equal("Неверный логин или пароль", ex.Message);
    }

    [Fact]
    public async Task Login_Success_ReturnsToken()
    {
        var user = User.Create("alice", "hash", UserRole.Admin);
        _users.Setup(r => r.GetByLoginAsync("alice", default)).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("ok", "hash")).Returns(true);
        _jwt.Setup(j => j.CreateToken(user.Id, user.Login, user.Role)).Returns("jwt-token");

        var result = await _sut.LoginAsync(new LoginRequest { Login = "alice", Password = "ok" });

        Assert.Equal("jwt-token", result.Token);
    }
}
