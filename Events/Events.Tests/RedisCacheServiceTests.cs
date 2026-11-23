using Events.Infrastructure.Caching;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;

namespace Events.Tests;

/// <summary>
/// Fail-soft поведение RedisCacheService: ошибки Redis не летят клиенту
/// </summary>
public class RedisCacheServiceTests
{
    /// <summary>
    /// При ошибке GET возвращаем null (промах), без исключения наружу
    /// </summary>
    [Fact]
    public async Task GetAsync_WhenRedisThrows_ReturnsDefault()
    {
        var db = new Mock<IDatabase>();
        db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new InvalidOperationException("redis down"));

        var mux = new Mock<IConnectionMultiplexer>();
        mux.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);

        var sut = new RedisCacheService(mux.Object, NullLogger<RedisCacheService>.Instance);

        var result = await sut.GetAsync<string>("event:test");

        Assert.Null(result);
    }

    /// <summary>
    /// При ошибке SET глотаем исключение — вызывающий код продолжает работу
    /// </summary>
    [Fact]
    public async Task SetAsync_WhenRedisThrows_DoesNotPropagate()
    {
        var db = new Mock<IDatabase>();
        db.Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ThrowsAsync(new InvalidOperationException("redis down"));

        var mux = new Mock<IConnectionMultiplexer>();
        mux.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);

        var sut = new RedisCacheService(mux.Object, NullLogger<RedisCacheService>.Instance);

        await sut.SetAsync("event:test", new { A = 1 }, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// При ошибке DEL тоже не пробрасываем
    /// </summary>
    [Fact]
    public async Task RemoveAsync_WhenRedisThrows_DoesNotPropagate()
    {
        var db = new Mock<IDatabase>();
        db.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new InvalidOperationException("redis down"));

        var mux = new Mock<IConnectionMultiplexer>();
        mux.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);

        var sut = new RedisCacheService(mux.Object, NullLogger<RedisCacheService>.Instance);

        await sut.RemoveAsync("event:test");
    }
}
