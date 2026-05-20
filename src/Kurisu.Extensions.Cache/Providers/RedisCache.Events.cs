using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Kurisu.Extensions.Cache.Providers;

public partial class RedisCache
{
    /// <summary>
    /// 添加注册事件
    /// </summary>
    private void AddRegisterEvent()
    {
        _connectionMultiplexer.ConnectionRestored += ConnMultiplexer_ConnectionRestored;
        _connectionMultiplexer.ConnectionFailed += ConnMultiplexer_ConnectionFailed;
        _connectionMultiplexer.ErrorMessage += ConnMultiplexer_ErrorMessage;
        _connectionMultiplexer.ConfigurationChanged += ConnMultiplexer_ConfigurationChanged;
        _connectionMultiplexer.HashSlotMoved += ConnMultiplexer_HashSlotMoved;
        _connectionMultiplexer.InternalError += ConnMultiplexer_InternalError;
        _connectionMultiplexer.ConfigurationChangedBroadcast += ConnMultiplexer_ConfigurationChangedBroadcast;
    }

    /// <summary>
    /// 重新配置广播时（通常意味着主从同步更改）
    /// </summary>
    private void ConnMultiplexer_ConfigurationChangedBroadcast(object sender, EndPointEventArgs e)
    {
        _logger.LogDebug("Redis 集群配置广播: {EndPoint}", e.EndPoint);
    }

    private void ConnMultiplexer_InternalError(object sender, InternalErrorEventArgs e)
    {
        _logger.LogError(e.Exception, "Redis 内部错误");
    }

    private void ConnMultiplexer_HashSlotMoved(object sender, HashSlotMovedEventArgs e)
    {
        _logger.LogDebug("Redis 哈希槽迁移: {OldEndPoint} -> {NewEndPoint}", e.OldEndPoint, e.NewEndPoint);
    }

    private void ConnMultiplexer_ConfigurationChanged(object sender, EndPointEventArgs e)
    {
        _logger.LogInformation("Redis 配置变更: {EndPoint}", e.EndPoint);
    }

    private void ConnMultiplexer_ErrorMessage(object sender, RedisErrorEventArgs e)
    {
        _logger.LogError("Redis 错误: {Message}", e.Message);
    }

    private void ConnMultiplexer_ConnectionFailed(object sender, ConnectionFailedEventArgs e)
    {
        _logger.LogError(e.Exception, "Redis 连接失败");
    }

    private void ConnMultiplexer_ConnectionRestored(object sender, ConnectionFailedEventArgs e)
    {
        _logger.LogInformation("Redis 连接恢复");
    }
}
