using Events.Application.Caching;
using Moq;

namespace Events.Tests;

/// <summary>
/// Инвалидация event:{id} — общий путь для CRUD и Kafka consumer
/// </summary>
public class EventCacheInvalidatorTests
{
    /// <summary>
    /// Invalidate удаляет правильный ключ через ICacheService
    /// </summary>
    [Fact]
    public async Task InvalidateEventAsync_RemovesEventKey()
    {
        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.RemoveAsync(It.IsAny<string>(), default)).Returns(Task.CompletedTask);
        var sut = new EventCacheInvalidator(cache.Object, new EventCacheGate());
        var id = Guid.NewGuid();

        await sut.InvalidateEventAsync(id);

        cache.Verify(c => c.RemoveAsync(CacheKeys.Event(id), default), Times.Once);
    }
}
