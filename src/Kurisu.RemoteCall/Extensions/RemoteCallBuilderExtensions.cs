using Microsoft.Extensions.DependencyInjection.Extensions;
using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Default;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// builder extensions
/// </summary>
public static class RemoteCallBuilderExtensions
{
    /// <summary>
    /// 添加标准结果处理器
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IRemoteCallBuilder AddStandardResultHandler(this IRemoteCallBuilder builder)
    {
        builder.Services.Replace(ServiceDescriptor.Singleton<IRemoteCallResultHandler, RemoteCallStandardResultHandler>());
        return builder;
    }
}