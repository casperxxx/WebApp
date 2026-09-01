using WebApp.Exceptions;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Tests;

public class EventServiceTests
{
    private readonly InMemoryEventStore _store = new();
    private readonly EventService _service;

    public EventServiceTests()
    {
        _service = new EventService(_store);
    }

    private static Event CreateEvent(string title, DateTime startAt, DateTime endAt, int totalSeats = 10)
    {
        return Event.Create(title, null, startAt, endAt, totalSeats);
    }

    [Fact]
    public void AddEvent_CreatesEvent()
    {
        var eventItem = CreateEvent("Встреча", new DateTime(2026, 7, 10, 10, 0, 0), new DateTime(2026, 7, 10, 12, 0, 0));

        _service.AddEvent(eventItem);

        Assert.Single(_store.Events);
        Assert.NotEqual(Guid.Empty, eventItem.Id);
    }

    [Fact]
    public async Task CreateEventAsync_SetsTotalAndAvailableSeats()
    {
        var request = new EventDTO
        {
            Title = "Концерт",
            Description = "Тест",
            StartAt = new DateTime(2026, 7, 10, 10, 0, 0),
            EndAt = new DateTime(2026, 7, 10, 12, 0, 0),
            TotalSeats = 25
        };

        var created = await _service.CreateEventAsync(request);

        Assert.Equal(25, created.TotalSeats);
        Assert.Equal(25, created.AvailableSeats);
        Assert.Single(_store.Events);
    }

    [Fact]
    public void GetEvents_ReturnsAll()
    {
        _service.AddEvent(CreateEvent("A", new DateTime(2026, 7, 10, 10, 0, 0), new DateTime(2026, 7, 10, 12, 0, 0)));
        _service.AddEvent(CreateEvent("B", new DateTime(2026, 7, 11, 10, 0, 0), new DateTime(2026, 7, 11, 12, 0, 0)));

        var result = _service.GetEvents(null, null, null, 1, 10);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public void GetEvent_ReturnsById()
    {
        var eventItem = CreateEvent("Test", new DateTime(2026, 7, 10, 10, 0, 0), new DateTime(2026, 7, 10, 12, 0, 0));
        _service.AddEvent(eventItem);

        var result = _service.GetEvent(eventItem.Id);

        Assert.Equal("Test", result.Title);
    }

    [Fact]
    public void UpdateEvent_UpdatesExisting()
    {
        var eventItem = CreateEvent("Old", new DateTime(2026, 7, 10, 10, 0, 0), new DateTime(2026, 7, 10, 12, 0, 0));
        _service.AddEvent(eventItem);
        var updated = CreateEvent("New", new DateTime(2026, 7, 10, 11, 0, 0), new DateTime(2026, 7, 10, 13, 0, 0));

        _service.UpdateEvent(eventItem.Id, updated);

        var result = _service.GetEvent(eventItem.Id);
        Assert.Equal("New", result.Title);
    }

    [Fact]
    public void DeleteEvent_RemovesEvent()
    {
        var eventItem = CreateEvent("Delete", new DateTime(2026, 7, 10, 10, 0, 0), new DateTime(2026, 7, 10, 12, 0, 0));
        _service.AddEvent(eventItem);

        _service.DeleteEvent(eventItem.Id);

        Assert.Empty(_store.Events);
    }

    [Fact]
    public void GetEvents_FilterByTitle()
    {
        _service.AddEvent(CreateEvent("Встреча", new DateTime(2026, 7, 10, 10, 0, 0), new DateTime(2026, 7, 10, 12, 0, 0)));
        _service.AddEvent(CreateEvent("Концерт", new DateTime(2026, 7, 11, 10, 0, 0), new DateTime(2026, 7, 11, 12, 0, 0)));

        var result = _service.GetEvents("встр", null, null, 1, 10);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Встреча", result.Items[0].Title);
    }

    [Fact]
    public void GetEvents_FilterByDates()
    {
        _service.AddEvent(CreateEvent("A", new DateTime(2026, 7, 10, 10, 0, 0), new DateTime(2026, 7, 10, 12, 0, 0)));
        _service.AddEvent(CreateEvent("B", new DateTime(2026, 7, 20, 10, 0, 0), new DateTime(2026, 7, 20, 12, 0, 0)));

        var result = _service.GetEvents(null, new DateTime(2026, 7, 15, 0, 0, 0), null, 1, 10);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("B", result.Items[0].Title);
    }

    [Fact]
    public void GetEvents_FilterByTo()
    {
        _service.AddEvent(CreateEvent("A", new DateTime(2026, 7, 10, 10, 0, 0), new DateTime(2026, 7, 10, 12, 0, 0)));
        _service.AddEvent(CreateEvent("B", new DateTime(2026, 7, 20, 10, 0, 0), new DateTime(2026, 7, 20, 12, 0, 0)));

        var result = _service.GetEvents(null, null, new DateTime(2026, 7, 15, 0, 0, 0), 1, 10);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("A", result.Items[0].Title);
    }

    [Fact]
    public void GetEvents_FilterByFromAndTo()
    {
        _service.AddEvent(CreateEvent("A", new DateTime(2026, 7, 5, 10, 0, 0), new DateTime(2026, 7, 5, 12, 0, 0)));
        _service.AddEvent(CreateEvent("B", new DateTime(2026, 7, 10, 10, 0, 0), new DateTime(2026, 7, 10, 12, 0, 0)));
        _service.AddEvent(CreateEvent("C", new DateTime(2026, 7, 20, 10, 0, 0), new DateTime(2026, 7, 20, 12, 0, 0)));

        var result = _service.GetEvents(
            null,
            new DateTime(2026, 7, 8, 0, 0, 0),
            new DateTime(2026, 7, 15, 0, 0, 0),
            1,
            10);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("B", result.Items[0].Title);
    }

    [Fact]
    public void GetEvents_WhitespaceTitle_Ignored()
    {
        _service.AddEvent(CreateEvent("A", new DateTime(2026, 7, 10, 10, 0, 0), new DateTime(2026, 7, 10, 12, 0, 0)));
        _service.AddEvent(CreateEvent("B", new DateTime(2026, 7, 11, 10, 0, 0), new DateTime(2026, 7, 11, 12, 0, 0)));

        var result = _service.GetEvents("   ", null, null, 1, 10);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public void GetEvents_Pagination()
    {
        for (var i = 1; i <= 15; i++)
        {
            _service.AddEvent(CreateEvent($"Event {i}", new DateTime(2026, 7, 10, 10, 0, 0), new DateTime(2026, 7, 10, 12, 0, 0)));
        }

        var page1 = _service.GetEvents(null, null, null, 1, 10);
        var page2 = _service.GetEvents(null, null, null, 2, 10);

        Assert.Equal(15, page1.TotalCount);
        Assert.Equal(10, page1.Items.Count);
        Assert.Equal(5, page2.Items.Count);
    }

    [Fact]
    public void GetEvents_CombinedFilters()
    {
        _service.AddEvent(CreateEvent("Встреча A", new DateTime(2026, 7, 10, 10, 0, 0), new DateTime(2026, 7, 10, 12, 0, 0)));
        _service.AddEvent(CreateEvent("Встреча B", new DateTime(2026, 7, 20, 10, 0, 0), new DateTime(2026, 7, 20, 12, 0, 0)));
        _service.AddEvent(CreateEvent("Концерт", new DateTime(2026, 7, 20, 10, 0, 0), new DateTime(2026, 7, 20, 12, 0, 0)));

        var result = _service.GetEvents("встр", new DateTime(2026, 7, 15, 0, 0, 0), null, 1, 10);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Встреча B", result.Items[0].Title);
    }

    [Fact]
    public void GetEvents_InvalidPage()
    {
        Assert.Throws<ArgumentException>(() => _service.GetEvents(null, null, null, 0, 10));
    }

    [Fact]
    public void GetEvents_InvalidPageSize()
    {
        Assert.Throws<ArgumentException>(() => _service.GetEvents(null, null, null, 1, 0));
    }

    [Fact]
    public void GetEvent_NotFound()
    {
        var id = Guid.NewGuid();

        Assert.Throws<NotFoundException>(() => _service.GetEvent(id));
    }

    [Fact]
    public void UpdateEvent_NotFound()
    {
        var eventItem = CreateEvent("Test", new DateTime(2026, 7, 10, 10, 0, 0), new DateTime(2026, 7, 10, 12, 0, 0));

        Assert.Throws<NotFoundException>(() => _service.UpdateEvent(Guid.NewGuid(), eventItem));
    }

    [Fact]
    public void DeleteEvent_NotFound()
    {
        Assert.Throws<NotFoundException>(() => _service.DeleteEvent(Guid.NewGuid()));
    }

    [Fact]
    public void AddEvent_InvalidDates()
    {
        var eventItem = CreateEvent("Bad", new DateTime(2026, 7, 10, 12, 0, 0), new DateTime(2026, 7, 10, 10, 0, 0));

        Assert.Throws<ArgumentException>(() => _service.AddEvent(eventItem));
    }

    [Fact]
    public void UpdateEvent_InvalidDates()
    {
        var eventItem = CreateEvent("Test", new DateTime(2026, 7, 10, 10, 0, 0), new DateTime(2026, 7, 10, 12, 0, 0));
        _service.AddEvent(eventItem);
        var invalid = CreateEvent("Bad", new DateTime(2026, 7, 10, 12, 0, 0), new DateTime(2026, 7, 10, 10, 0, 0));

        Assert.Throws<ArgumentException>(() => _service.UpdateEvent(eventItem.Id, invalid));
    }
}
