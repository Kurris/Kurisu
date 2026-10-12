using Kurisu.AspNetCore.Abstractions.DistributedLock.Impl;
using Kurisu.AspNetCore.Abstractions.DistributedLock;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kurisu.Extensions.Cache;

/// <summary>
/// 分布式锁配置服务注册。
/// </summary>
public static class DistributedLockServiceCollectionExtensions
{
    /// <summary>
    /// 注册默认锁配置工厂，保留应用已注册的自定义工厂。
    /// 此方法不注册锁提供者，也不连接 Redis。
    /// </summary>
    public static IServiceCollection AddDistributedLockOptions(this IServiceCollection services)
    {
        services.TryAddSingleton<IDistributedLockOptionsFactory, DefaultDistributedLockOptionsFactory>();
        return services;
    }
}
