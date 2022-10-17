using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Grpc.Abstractions
{
    /// <summary>
    /// Grpc模块
    /// </summary>
    public interface IGrpcModule : ISingletonDependency
    {
        /// <summary>
        /// 配置key
        /// <remarks>
        /// GrpcSetting.ServiceRoutes的key值
        /// </remarks>
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// 模块名称
        /// </summary>
        public string DisplayName { get; set; }
    }
}