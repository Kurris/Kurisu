using System.Collections.Generic;
using Kurisu.ConfigurableOptions.Attributes;

namespace Kurisu.Grpc
{
    /// <summary>
    /// Grpc配置
    /// <remarks>
    /// Grpc使用http2.0通道,需要单独设置http1.0和http2.0的端口
    /// </remarks>
    /// </summary>
    [Configuration]
    public class GrpcSetting
    {
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enable { get; set; }

        /// <summary>
        /// Http端口
        /// </summary>
        public int HttpPort { get; set; }

        /// <summary>
        /// Grpc端口
        /// </summary>
        public int GrpcPort { get; set; }

        /// <summary>
        /// 服务路由
        /// <remarks>
        /// 通过appsettings.json获取
        /// </remarks>
        /// </summary>
        // ReSharper disable once CollectionNeverUpdated.Global
        public IDictionary<string, string> ServiceRoutes { get; set; }
    }
}