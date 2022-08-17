using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Grpc.Abstractions
{
    /// <summary>
    /// Grpc客户端服务
    /// </summary>
    /// <typeparam name="TGrpcModule">Grpc远程模块</typeparam>
    /// <typeparam name="TClient">Grpc客户端类型</typeparam>
    // ReSharper disable once UnusedTypeParameter
    // ReSharper disable once TypeParameterCanBeVariant
    public interface IGrpcClientService<TGrpcModule, TClient> : ISingletonDependency where TClient : ClientBase where TGrpcModule : IGrpcModule
    {
        /// <summary>
        /// 创建Grpc客户端
        /// </summary>
        /// <returns></returns>
        TClient Create();
    }
}