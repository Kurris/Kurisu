using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Grpc.Abstractions
{
    /// <summary>
    /// Grpc被调用模块
    /// </summary>
    public interface IGrpcModule : ISingletonDependency
    {
        /// <summary>
        /// 配置key
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// 模块名称
        /// </summary>
        public string DisplayName { get; set; }
    }
}