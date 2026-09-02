using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.DataAccess;
using WebApp.Exceptions;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Tests;

public class EventServiceTests : IDisposable
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    private readonly ServiceProvider _serviceProvider;

    public EventServiceTests()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_dbName));
        services.AddScoped<IEventService, EventService>();
        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    private IEventService CreateService(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IEventService>();

    private static EventDTO CreateRequest(
        string title,
        DateTime startAt,
        DateTime endAt,
        int totalSeats = 10)
    {
        return new EventDTO
        {
            Title = title,
            Description = null,
            StartAt = startAt,
            EndAt = endAt,
            TotalSeats = totalSeats
        };
    }

    [Fact]
    public async Task CreateEventAsync_CreatesEvent()
    {
        using var scope = _serviceProvider.CreateScope();
        var service = CreateService(scope);
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var created = await service.CreateEventAsync(CreateRequest(
            "Встреча",
            new DateTime(2026, 7, 10, 10, 0, 0),
            new DateTime(2026, 7, 10, 12, 0, 0)));

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Single(context.Events);
    }

    [Fact]
    public async Task CreateEventAsync_SetsTotalAndAvailableSeats()
    {
        using var scope = _serviceProvider.CreateScope();
        var service = CreateService(scope);
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var created = await service.CreateEventAsync(CreateRequest(
            "Концерт",
            new DateTime(2026, 7, 10, 10, 0, 0),
            new DateTime(2026, 7, 10, 12, 0, 0),
            totalSeats: 25));

        Assert.Equal(25, created.TotalSeats);
        Assert.Equal(25, created.AvailableSeats);
        Assert.Single(context.Events);
    }

    [Fact]
    public async Task GetEventsAsync_ReturnsAll()
    {
        using var scope = _serviceProvider.CreateScope();
        var service = CreateService(scope);

        await service.CreateEventAsync(CreateRequest("A", new DateTime(2026, 7, 10, 10, 0, 0), new DateTime(2026, 7, 10, 12, 0, 0)));
        await service.CreateEventAsync(CreateRequest("B", new DateTime(2026, 7, 11, 10, 0, 0), new DateTime(2026, 7, 11, 12, 0, 0)));

        var result = await service.GetEventsAsync(null, null, null, 1, 10);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetEventAsync_ReturnsById()
    {
        using var scope = _serviceProvider.CreateScope();
        var service = CreateService(scope);

        var created = await service.CreateEventAsync(CreateRequest(
            "Test",
            new DateTime(2026, 7, 10, 10, 0, 0),
            new DateTime(2026, 7, 10, 12, 0, 0)));

        var result = await service.GetEventAsync(created.Id);

        Assert.Equal("Test", result.Title);
    }

    [Fact]
    public async Task UpdateEventAsync_UpdatesExisting()
    {
        using var scope = _serviceProvider.CreateScope();
        var service = CreateService(scope);

        var created = await service.CreateEventAsync(CreateRequest(
            "Old",
            new DateTime(2026, 7, 10, 10, 0, 0),
            new DateTime(2026, 7, 10, 12, 0, 0)));

        var updated = Event.FromUpdate(CreateRequest(
            "New",
            new DateTime(2026, 7, 10, 11, 0, 0),
            new DateTime(2026, 7, 10, 13, 0, 0)));

        await service.UpdateEventAsync(created.Id, updated);

        var result = await service.GetEventAsync(created.Id);
        Assert.Equal("New", result.Title);
    }

    [Fact]
    public async Task DeleteEventAsync_RemovesEvent()
    {
        using var scope = _serviceProvider.CreateScope();
        var service = CreateService(scope);
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var created = await service.CreateEventAsync(CreateRequest(
            "Delete",
            new DateTime(2026, 7, 10, 10, 0, 0),
            new DateTime(2026, 7, 10, 12, 0, 0)));

        await service.DeleteEventAsync(created.Id);

        Assert.Empty(context.Events);
    }

    [Fact]
    public async Task GetEventsAsync_FilterByTitle()
    {
        using var scope = _serviceProvider.CreateScope();
        var service = CreateService(scope);

        await service.CreateEventAsync(CreateRequest("Встреча", new DateTime(2026, 7, 10, 10, 0, 0), new DateTime(2026, 7, 10, 12, 0, 0)));
        await service.CreateEventAsync(CreateRequest("Концерт", new DateTime(2026, 7, 11, 10, 0, 0), new DateTime(2026, 7, 11, 12, 0, 0)));

        var result = await service.GetEventsAsync("встр", null, null, 1, 10);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Встреча", result.Items[0].Title);
    }

    [Fact]
    public async Task GetEventsAsync_FilterByDates()
    {
        using var scope = _serviceProvider.CreateScope();
        var service = CreateService(scope);

        await service.CreateEventAsync(CreateRequest("A", new DateTime(2026, 7, 10, 10, 0, 0), new DateTime(2026, 7, 10, 12, 0, 0)));
        await service.CreateEventAsync(CreateRequest("B", new DateTime(2026, 7, 20, 10, 0, 0), new DateTime(2026, 7, 20, 12, 0, 0)));

        var result = await service.GetEventsAsync(null, new DateTime(2026, 7, 15, 0, 0, 0), null, 1, 10);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("B", result.Items[0].Title);
    }

    [Fact]
    public async Task GetEventsAsync_FilterByTo()
    {
        using var scope = _serviceProvider.CreateScope();
        var service = CreateService(scope);

        await service.CreateEventAsync(CreateRequest("A", new DateTime(2026, 7, 10, 10, 0, 0), new DateTime(2026, 7, 10, 12, 0, 0)));
        await service.CreateEventAsync(CreateRequest("B", new DateTime(2026, 7, 20, 10, 0, 0), new DateTime(2026, 7, 20, 12, 0, 0)));

        var result = await service.GetEventsAsync(null, null, new DateTime(2026, 7, 15, 0, 0, 0), 1, 10);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("A", result.Items[0].Title);
    }

    [Fact]
    public async Task GetEventsAsync_FilterByFromAndTo()
    {
        using var scope = _serviceProvider.CreateScope();
        var service = CreateService(scope);

        await service.CreateEventAsync(CreateRequest("A", new DateTime(2026, 7, 5, 10, 0, 0), new DateTime(2026, 7, 5, 12, 0, 0)));
        await service.CreateEventAsync(CreateRequest("B", new DateTime(2026, 7, 10, 10, 0, 0), new DateTime(2026, 7, 10, 12, 0, 0)));
        await service.CreateEventAsync(CreateRequest("C", new DateTime(2026, 7, 20, 10, 0, 0), new DateTime(2026, 7, 20, 12, 0, 0)));

        var result = await service.GetEventsAsync(
            null,
            new DateTime(2026, 7, 8, 0, 0, 0),
            new DateTime(2026, 7, 15, 0, 0, 0),
            1,
            10);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("B", result.Items[0].Title);
    }

    [Fact]
    public async Task GetEventsAsync_WhitespaceTitle_Ignored()
    {
        using var scope = _serviceProvider.CreateScope();
        var service = CreateService(scope);

        await service.CreateEventAsync(CreateRequest("A", new DateTime(2026, 7, 10, 10, 0, 0), new DateTime(2026, 7, 10, 12, 0, 0)));
        await service.CreateEventAsync(CreateRequest("B", new DateTime(2026, 7, 11, 10, 0, 0), new DateTime(2026, 7, 11, 12, 0, 0)));

        var result = await service.GetEventsAsync("   ", null, null, 1, 10);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetEventsAsync_Pagination()
    {
        using var scope = _serviceProvider.CreateScope();
        var service = CreateService(scope);

        for (var i = 1; i <= 15; i++)
        {
            await service.CreateEventAsync(CreateRequest(
                $"Event {i}",
                new DateTime(2026, 7, 10, 10, 0, 0),
                new DateTime(2026, 7, 10, 12, 0, 0)));
        }

        var page1 = await service.GetEventsAsync(null, null, null, 1, 10);
        var page2 = await service.GetEventsAsync(null, null, null, 2, 10);

        Assert.Equal(15, page1.TotalCount);
        Assert.Equal(10, page1.Items.Count);
        Assert.Equal(5, page2.Items.Count);
    }

    [Fact]
    public async Task GetEventsAsync_CombinedFilters()
    {
        using var scope = _serviceProvider.CreateScope();
        var service = CreateService(scope);

        await service.CreateEventAsync(CreateRequest("Встреча A", new DateTime(2026, 7, 10, 10, 0, 0), new DateTime(2026, 7, 10, 12, 0, 0)));
        await service.CreateEventAsync(CreateRequest("Встреча B", new DateTime(2026, 7, 20, 10, 0, 0), new DateTime(2026, 7, 20, 12, 0, 0)));
        await service.CreateEventAsync(CreateRequest("Концерт", new DateTime(2026, 7, 20, 10, 0, 0), new DateTime(2026, 7, 20, 12, 0, 0)));

        var result = await service.GetEventsAsync("встр", new DateTime(2026, 7, 15, 0, 0, 0), null, 1, 10);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Встреча B", result.Items[0].Title);
    }

    [Fact]
    public async Task GetEventsAsync_InvalidPage()
    {
        using var scope = _serviceProvider.CreateScope();
        var service = CreateService(scope);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetEventsAsync(null, null, null, 0, 10));
    }

    [Fact]
    public async Task GetEventsAsync_InvalidPageSize()
    {
        using var scope = _serviceProvider.CreateScope();
        var service = CreateService(scope);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetEventsAsync(null, null, null, 1, 0));
    }

    [Fact]
    public async Task GetEventAsync_NotFound()
    {
        using var scope = _serviceProvider.CreateScope();
        var service = CreateService(scope);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetEventAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateEventAsync_NotFound()
    {
        using var scope = _serviceProvider.CreateScope();
        var service = CreateService(scope);

        var eventItem = Event.FromUpdate(CreateRequest(
            "Test",
            new DateTime(2026, 7, 10, 10, 0, 0),
            new DateTime(2026, 7, 10, 12, 0, 0)));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UpdateEventAsync(Guid.NewGuid(), eventItem));
    }

    [Fact]
    public async Task DeleteEventAsync_NotFound()
    {
        using var scope = _serviceProvider.CreateScope();
        var service = CreateService(scope);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.DeleteEventAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateEventAsync_InvalidDates()
    {
        using var scope = _serviceProvider.CreateScope();
        var service = CreateService(scope);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateEventAsync(CreateRequest(
            "Bad",
            new DateTime(2026, 7, 10, 12, 0, 0),
            new DateTime(2026, 7, 10, 10, 0, 0))));
    }

    [Fact]
    public async Task UpdateEventAsync_InvalidDates()
    {
        using var scope = _serviceProvider.CreateScope();
        var service = CreateService(scope);

        var created = await service.CreateEventAsync(CreateRequest(
            "Test",
            new DateTime(2026, 7, 10, 10, 0, 0),
            new DateTime(2026, 7, 10, 12, 0, 0)));

        var invalid = Event.FromUpdate(CreateRequest(
            "Bad",
            new DateTime(2026, 7, 10, 12, 0, 0),
            new DateTime(2026, 7, 10, 10, 0, 0)));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateEventAsync(created.Id, invalid));
    }
}
