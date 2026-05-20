using System.Net;
using Kurisu.Extensions.Cache.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Kurisu.Test.Cache;

[Trait("feature", "event-handlers")]
public class RedisCacheEventHandlerTests
{
    private static (RedisCache cache, Mock<IConnectionMultiplexer> mockConn) CreateCache()
    {
        var mockConn = new Mock<IConnectionMultiplexer>();
        var mockDb = new Mock<IDatabase>();
        mockConn.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDb.Object);
        var mockLogger = new Mock<ILogger<RedisCache>>();
        var cache = new RedisCache(mockConn.Object, mockLogger.Object);
        return (cache, mockConn);
    }

    private static EndPoint Ep => new DnsEndPoint("127.0.0.1", 6379);

    [Fact(DisplayName = "ConfigurationChanged事件处理器")]
    public void ConnMultiplexer_ConfigurationChanged_ShouldLog()
    {
        var (cache, mockConn) = CreateCache();
        mockConn.Raise(c => c.ConfigurationChanged += null,
            null!, new EndPointEventArgs(null!, Ep));
        cache.Dispose();
    }

    [Fact(DisplayName = "ConfigurationChangedBroadcast事件处理器")]
    public void ConnMultiplexer_ConfigurationChangedBroadcast_ShouldLog()
    {
        var (cache, mockConn) = CreateCache();
        mockConn.Raise(c => c.ConfigurationChangedBroadcast += null,
            null!, new EndPointEventArgs(null!, Ep));
        cache.Dispose();
    }

    [Fact(DisplayName = "ConnectionFailed事件处理器")]
    public void ConnMultiplexer_ConnectionFailed_ShouldLog()
    {
        var (cache, mockConn) = CreateCache();
        mockConn.Raise(c => c.ConnectionFailed += null,
            null!, new ConnectionFailedEventArgs(null!, Ep, ConnectionType.Interactive, ConnectionFailureType.UnableToConnect, new Exception("test"), "test"));
        cache.Dispose();
    }

    [Fact(DisplayName = "ConnectionRestored事件处理器")]
    public void ConnMultiplexer_ConnectionRestored_ShouldLog()
    {
        var (cache, mockConn) = CreateCache();
        mockConn.Raise(c => c.ConnectionRestored += null,
            null!, new ConnectionFailedEventArgs(null!, Ep, ConnectionType.Interactive, ConnectionFailureType.UnableToConnect, new Exception("test"), "test"));
        cache.Dispose();
    }

    [Fact(DisplayName = "ErrorMessage事件处理器")]
    public void ConnMultiplexer_ErrorMessage_ShouldLog()
    {
        var (cache, mockConn) = CreateCache();
        mockConn.Raise(c => c.ErrorMessage += null,
            null!, new RedisErrorEventArgs(null!, Ep, "test error"));
        cache.Dispose();
    }

    [Fact(DisplayName = "HashSlotMoved事件处理器")]
    public void ConnMultiplexer_HashSlotMoved_ShouldLog()
    {
        var (cache, mockConn) = CreateCache();
        mockConn.Raise(c => c.HashSlotMoved += null,
            null!, new HashSlotMovedEventArgs(null!, 0, Ep, Ep));
        cache.Dispose();
    }

    [Fact(DisplayName = "InternalError事件处理器")]
    public void ConnMultiplexer_InternalError_ShouldLog()
    {
        var (cache, mockConn) = CreateCache();
        mockConn.Raise(c => c.InternalError += null,
            null!, new InternalErrorEventArgs(null!, Ep, ConnectionType.Interactive, new Exception("test internal"), "test"));
        cache.Dispose();
    }
}
