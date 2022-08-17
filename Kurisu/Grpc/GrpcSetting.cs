using System.Collections.Generic;
using Kurisu.ConfigurableOptions.Attributes;

namespace Kurisu.Grpc
{
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
        /// </summary>
        // ReSharper disable once CollectionNeverUpdated.Global
        public IDictionary<string, string> ServiceRoutes { get; set; }
    }
}