using System.Collections.Generic;
using Kurisu.AspNetCore.ConfigurableOptions.Attributes;

namespace Kurisu.AspNetCore.Grpc;

/// <summary>
/// Grpc配置
/// <remarks>
/// Grpc使用http2.0通道,需要单独设置http1.0和http2.0的端口
/// </remarks>
/// </summary>
[Configuration]
public class GrpcOptions
{
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable { get; set; }

    /// <summary>
    /// 使用Tls
    /// </summary>
    public bool UseTls { get; set; }

    /// <summary>
    /// grpc端口
    /// </summary>
    public int GrpcPort { get; set; }

    /// <summary>
    /// http端口
    /// </summary>
    public int HttpPort { get; set; }

    /// <summary>
    /// 服务路由
    /// <remarks>
    /// 通过appsettings.json获取
    /// </remarks>
    /// </summary>
    // ReSharper disable once CollectionNeverUpdated.Global
    public IDictionary<string, string> ServiceRoutes { get; set; }
}