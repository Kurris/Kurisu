using Kurisu.AspNetCore.Abstractions.Cache;
using Kurisu.Extensions.Cache.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Kurisu.Test.Cache.CacheData;

public class RedisCacheBatchDeleteTests
{
    private static RedisCache Create(Mock<IDatabase> db, Mock<IConnectionMultiplexer> connection, bool cluster = false)
    {
        connection.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);
        var server = new Mock<IServer>();
        server.SetupGet(s => s.ServerType).Returns(cluster ? ServerType.Cluster : ServerType.Standalone);
        connection.Setup(c => c.GetServers()).Returns([server.Object]);
        return new RedisCache(connection.Object, NullLogger<RedisCache>.Instance);
    }

    [Fact]
    public async Task Standalone_SubmitsOneDistinctBatch_AndReturnsRemovedCount()
    {
        var db = new Mock<IDatabase>(MockBehavior.Strict);
        var connection = new Mock<IConnectionMultiplexer>();
        db.Setup(d => d.KeyDeleteAsync(It.Is<RedisKey[]>(keys => keys.SequenceEqual(new RedisKey[] { "a", "b", "missing" })), CommandFlags.None))
            .ReturnsAsync(2);
        using var provider = Create(db, connection);
        ICache cache = provider;
        Assert.Equal(2, await cache.RemoveManyAsync(["a", "b", "a", "missing"]));
        db.Verify(d => d.KeyDeleteAsync(It.IsAny<RedisKey[]>(), CommandFlags.None), Times.Once);
        db.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task InvalidBatch_IsRejectedBeforeSendingAnything(int kind)
    {
        var db = new Mock<IDatabase>(MockBehavior.Strict);
        using var provider = Create(db, new Mock<IConnectionMultiplexer>());
        ICache cache = provider;
        IEnumerable<string> keys = kind switch { 0 => null!, 1 => ["a", " "], _ => ["a", null!] };
        await Assert.ThrowsAnyAsync<ArgumentException>(() => cache.RemoveManyAsync(keys));
        db.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EmptyBatch_DoesNotSendRedisCommand()
    {
        var db = new Mock<IDatabase>(MockBehavior.Strict);
        using var provider = Create(db, new Mock<IConnectionMultiplexer>());
        Assert.Equal(0, await ((ICache)provider).RemoveManyAsync([]));
        db.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Cluster_GroupsBySlot_AndStartsAllGroupsBeforeWaiting()
    {
        var db = new Mock<IDatabase>(MockBehavior.Strict);
        var connection = new Mock<IConnectionMultiplexer>();
        connection.Setup(c => c.GetHashSlot(It.IsAny<RedisKey>())).Returns((RedisKey key) => key.ToString().StartsWith('a') ? 1 : 2);
        var first = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var batches = new List<string[]>();
        db.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey[]>(), CommandFlags.None)).Returns((RedisKey[] keys, CommandFlags _) =>
        {
            batches.Add(keys.Select(k => k.ToString()).ToArray());
            if (batches.Count == 2) started.SetResult();
            return keys[0] == (RedisKey)"a1" ? first.Task : second.Task;
        });
        using var provider = Create(db, connection, cluster: true);
        var deletion = ((ICache)provider).RemoveManyAsync(["a1", "b1", "a2"]);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(new[] { "a1", "a2" }, batches[0]);
        Assert.Equal(new[] { "b1" }, batches[1]);
        first.SetResult(2);
        second.SetResult(1);
        Assert.Equal(3, await deletion);
        db.Verify(d => d.KeyDeleteAsync(It.IsAny<RedisKey[]>(), CommandFlags.None), Times.Exactly(2));
        db.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Cluster_FailingGroup_DoesNotPreventOtherGroupsFromBeingSubmitted()
    {
        var db = new Mock<IDatabase>(MockBehavior.Strict);
        var connection = new Mock<IConnectionMultiplexer>();
        connection.Setup(c => c.GetHashSlot(It.IsAny<RedisKey>())).Returns((RedisKey key) => key == (RedisKey)"a" ? 1 : 2);
        db.Setup(d => d.KeyDeleteAsync(It.Is<RedisKey[]>(keys => keys[0] == (RedisKey)"a"), CommandFlags.None))
            .Throws(new IOException("delete failed"));
        db.Setup(d => d.KeyDeleteAsync(It.Is<RedisKey[]>(keys => keys[0] == (RedisKey)"b"), CommandFlags.None)).ReturnsAsync(1);
        using var provider = Create(db, connection, cluster: true);
        await Assert.ThrowsAsync<IOException>(() => provider.RemoveManyAsync(["a", "b"]));
        db.Verify(d => d.KeyDeleteAsync(It.Is<RedisKey[]>(keys => keys[0] == (RedisKey)"b"), CommandFlags.None), Times.Once);
    }
}