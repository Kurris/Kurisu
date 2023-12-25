using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Grpc.Abstractions;

/// <summary>
/// Grpc模块
/// </summary>
public interface IGrpcModule : ISingletonDependency
{
    /// <summary>
    /// 配置服务名称
    /// <remarks>
    /// GrpcSetting.ServiceRoutes的key值
    /// </remarks>
    /// </summary>
    public string Service { get; set; }

    /// <summary>
    /// 模块名称
    /// </summary>
    public string DisplayName { get; set; }
}