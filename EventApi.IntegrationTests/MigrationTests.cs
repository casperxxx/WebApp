using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace EventApi.IntegrationTests;

[Collection("Postgres")]
public class MigrationTests
{
    private readonly PostgresFixture _fixture;

    public MigrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Migrate_CreatesEventsAndBookingsTablesWithForeignKey()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateDbContext();

        // Act — читаем схему из PostgreSQL
        await using var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_name IN ('events', 'bookings')
            ORDER BY table_name;
            """;

        var tables = new List<string>();
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }

        command.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.table_constraints
            WHERE table_name = 'bookings'
              AND constraint_type = 'FOREIGN KEY'
              AND constraint_name = 'FK_bookings_events_EventId';
            """;
        var fkCount = (long)(await command.ExecuteScalarAsync())!;

        // Assert
        Assert.Equal(["bookings", "events"], tables);
        Assert.Equal(1, fkCount);
    }
}
