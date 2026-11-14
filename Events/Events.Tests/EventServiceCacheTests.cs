using Events.Application.Caching;
using Events.Application.Models;
using Events.Application.Repositories;
using Events.Application.Services;
using Events.Domain.Entities;
using Moq;

namespace Events.Tests;

/// <summary>
/// Юнит-тесты Cache-Aside в EventService: hit, miss и инвалидация после записи
/// </summary>
public class EventServiceCacheTests
{
    private readonly Mock<IEventRepository> _events = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICacheService> _cache = new();

    /// <summary>
    /// TTL как в appsettings (для проверки Set с нужным временем жизни)
    /// </summary>
    private readonly RedisCacheOptions _options = new()
    {
        EventTtlSeconds = 300,
        TopTtlSeconds = 60
    };

    /// <summary>
    /// Собираем сервис с моками репозитория и кеша
    /// </summary>
    private EventService CreateSut() =>
        new(_events.Object, _uow.Object, _cache.Object, _options);

    /// <summary>
    /// Вспомогательный метод: доменное событие для Arrange
    /// </summary>
    private static Event CreateEvent(string title = "Concert") =>
        Event.Create(
            title,
            null,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(2),
            10);

    /// <summary>
    /// Cache hit: данные уже в Redis — в БД не ходим и Set не вызываем
    /// </summary>
    [Fact]
    public async Task GetEventAsync_CacheHit_DoesNotCallRepository()
    {
        var id = Guid.NewGuid();
        var cached = new EventResponse
        {
            Id = id,
            Title = "From cache",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(1).AddHours(1),
            TotalSeats = 10,
            AvailableSeats = 8
        };

        // кеш сразу отдаёт значение
        _cache.Setup(c => c.GetAsync<EventResponse>(CacheKeys.Event(id), default))
            .ReturnsAsync(cached);

        var result = await CreateSut().GetEventAsync(id);

        Assert.Equal("From cache", result.Title);
        _events.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), default), Times.Never);
        _cache.Verify(
            c => c.SetAsync(It.IsAny<string>(), It.IsAny<EventResponse>(), It.IsAny<TimeSpan>(), default),
            Times.Never);
    }

    /// <summary>
    /// Cache miss: Redis пустой → читаем из БД и кладём в кеш с TTL события
    /// </summary>
    [Fact]
    public async Task GetEventAsync_CacheMiss_LoadsFromDbAndSetsCache()
    {
        var ev = CreateEvent();
        _cache.Setup(c => c.GetAsync<EventResponse>(CacheKeys.Event(ev.Id), default))
            .ReturnsAsync((EventResponse?)null);
        _events.Setup(r => r.GetByIdAsync(ev.Id, default)).ReturnsAsync(ev);
        _cache.Setup(c => c.SetAsync(
                CacheKeys.Event(ev.Id),
                It.IsAny<EventResponse>(),
                _options.EventTtl,
                default))
            .Returns(Task.CompletedTask);

        var result = await CreateSut().GetEventAsync(ev.Id);

        Assert.Equal(ev.Id, result.Id);
        Assert.Equal(ev.Title, result.Title);
        _events.Verify(r => r.GetByIdAsync(ev.Id, default), Times.Once);
        _cache.Verify(
            c => c.SetAsync(
                CacheKeys.Event(ev.Id),
                It.Is<EventResponse>(x => x.Id == ev.Id),
                _options.EventTtl,
                default),
            Times.Once);
    }

    /// <summary>
    /// Hit для топ-10: список берём из events:top10, репозиторий не трогаем
    /// </summary>
    [Fact]
    public async Task GetTopEventsAsync_CacheHit_DoesNotCallRepository()
    {
        var cached = new List<EventResponse>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Top1",
                StartAt = DateTime.UtcNow.AddDays(1),
                EndAt = DateTime.UtcNow.AddDays(1).AddHours(1),
                TotalSeats = 10,
                AvailableSeats = 1
            }
        };

        _cache.Setup(c => c.GetAsync<List<EventResponse>>(CacheKeys.Top10, default))
            .ReturnsAsync(cached);

        var result = await CreateSut().GetTopEventsAsync();

        Assert.Single(result);
        Assert.Equal("Top1", result[0].Title);
        _events.Verify(r => r.GetTopBySoldRatioAsync(It.IsAny<int>(), default), Times.Never);
    }

    /// <summary>
    /// Miss для топ-10: грузим из БД и пишем в Redis с TopTtl
    /// </summary>
    [Fact]
    public async Task GetTopEventsAsync_CacheMiss_LoadsFromDbAndSetsCache()
    {
        var items = new List<Event> { CreateEvent("A"), CreateEvent("B") };
        _cache.Setup(c => c.GetAsync<List<EventResponse>>(CacheKeys.Top10, default))
            .ReturnsAsync((List<EventResponse>?)null);
        _events.Setup(r => r.GetTopBySoldRatioAsync(10, default)).ReturnsAsync(items);
        _cache.Setup(c => c.SetAsync(
                CacheKeys.Top10,
                It.IsAny<List<EventResponse>>(),
                _options.TopTtl,
                default))
            .Returns(Task.CompletedTask);

        var result = await CreateSut().GetTopEventsAsync();

        Assert.Equal(2, result.Count);
        _events.Verify(r => r.GetTopBySoldRatioAsync(10, default), Times.Once);
        _cache.Verify(
            c => c.SetAsync(
                CacheKeys.Top10,
                It.Is<List<EventResponse>>(x => x.Count == 2),
                _options.TopTtl,
                default),
            Times.Once);
    }

    /// <summary>
    /// После Update сбрасываем ключ event:{id}, чтобы не отдавать устаревшие данные
    /// </summary>
    [Fact]
    public async Task UpdateEventAsync_AfterSave_RemovesEventCacheKey()
    {
        var ev = CreateEvent();
        _events.Setup(r => r.GetByIdAsync(ev.Id, default)).ReturnsAsync(ev);
        _uow.Setup(u => u.SaveChangesAsync(default)).Returns(Task.CompletedTask);
        _cache.Setup(c => c.RemoveAsync(CacheKeys.Event(ev.Id), default))
            .Returns(Task.CompletedTask);

        var dto = new EventDTO
        {
            Title = "Updated",
            Description = "d",
            StartAt = ev.StartAt,
            EndAt = ev.EndAt,
            TotalSeats = ev.TotalSeats
        };

        await CreateSut().UpdateEventAsync(ev.Id, dto);

        // сначала БД, потом Remove — иначе можно вернуть старый кеш
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
        _cache.Verify(c => c.RemoveAsync(CacheKeys.Event(ev.Id), default), Times.Once);
    }

    /// <summary>
    /// После Delete тоже удаляем ключ из Redis
    /// </summary>
    [Fact]
    public async Task DeleteEventAsync_AfterSave_RemovesEventCacheKey()
    {
        var ev = CreateEvent();
        _events.Setup(r => r.GetByIdAsync(ev.Id, default)).ReturnsAsync(ev);
        _uow.Setup(u => u.SaveChangesAsync(default)).Returns(Task.CompletedTask);
        _cache.Setup(c => c.RemoveAsync(CacheKeys.Event(ev.Id), default))
            .Returns(Task.CompletedTask);

        await CreateSut().DeleteEventAsync(ev.Id);

        _events.Verify(r => r.Delete(ev), Times.Once);
        _cache.Verify(c => c.RemoveAsync(CacheKeys.Event(ev.Id), default), Times.Once);
    }

    /// <summary>
    /// После Create вызываем Remove (ключа обычно ещё нет — это безопасно)
    /// </summary>
    [Fact]
    public async Task CreateEventAsync_AfterSave_RemovesEventCacheKey()
    {
        _events.Setup(r => r.AddAsync(It.IsAny<Event>(), default)).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(default)).Returns(Task.CompletedTask);
        _cache.Setup(c => c.RemoveAsync(It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);

        var dto = new EventDTO
        {
            Title = "New",
            StartAt = DateTime.UtcNow.AddDays(2),
            EndAt = DateTime.UtcNow.AddDays(2).AddHours(2),
            TotalSeats = 5
        };

        var created = await CreateSut().CreateEventAsync(dto);

        _cache.Verify(c => c.RemoveAsync(CacheKeys.Event(created.Id), default), Times.Once);
    }
}
