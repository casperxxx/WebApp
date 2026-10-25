using WebApp.Domain.Entities;
using WebApp.Infrastructure.DataAccess;
using WebApp.Infrastructure.Repositories;

namespace EventApi.IntegrationTests;

/// <summary>
/// Интеграционные тесты репозитория пользователей (уникальность логина)
/// </summary>
[Collection("Postgres")]
public class UserRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public UserRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_DuplicateLogin_ThrowsOnSave()
    {
        await _fixture.ResetDatabaseAsync();
        var context = _fixture.CreateDbContext();
        await using (context)
        {
            var users = new UserRepository(context);
            var unitOfWork = new UnitOfWork(context);

            await users.AddAsync(User.Create("same_login", "HASH1", UserRole.User));
            await unitOfWork.SaveChangesAsync();

            await users.AddAsync(User.Create("same_login", "HASH2", UserRole.Admin));

            await Assert.ThrowsAnyAsync<Exception>(() => unitOfWork.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task GetByLoginAsync_ReturnsUser()
    {
        await _fixture.ResetDatabaseAsync();
        var context = _fixture.CreateDbContext();
        await using (context)
        {
            var users = new UserRepository(context);
            var unitOfWork = new UnitOfWork(context);

            var created = User.Create("finder", "HASH", UserRole.User);
            await users.AddAsync(created);
            await unitOfWork.SaveChangesAsync();

            var loaded = await users.GetByLoginAsync("finder");
            Assert.NotNull(loaded);
            Assert.Equal(created.Id, loaded.Id);
            Assert.True(await users.ExistsByLoginAsync("finder"));
            Assert.False(await users.ExistsByLoginAsync("missing"));
        }
    }
}
