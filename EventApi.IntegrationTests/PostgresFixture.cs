using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using WebApp.DataAccess;
using WebApp.Repositories;

namespace EventApi.IntegrationTests;

/// <summary>
/// Один контейнер PostgreSQL на все интеграционные тесты
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("eventapi_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    /// <summary>
    /// Чистая БД перед каждым тестом: удаляем и накатываем миграции
    /// </summary>
    internal async Task ResetDatabaseAsync()
    {
        await using var context = CreateDbContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    internal AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    internal (IEventRepository Events, IBookingRepository Bookings, AppDbContext Context) CreateRepositories()
    {
        var context = CreateDbContext();
        return (new EventRepository(context), new BookingRepository(context), context);
    }
}

[CollectionDefinition("Postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
}
