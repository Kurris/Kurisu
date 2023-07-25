using Kurisu.Authentication.Abstractions;
using Kurisu.Authentication.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 提供获取当前租户的能力
/// </summary>
public static class CurrentTenantInfoExtensions
{
    public static IServiceCollection AddKurisuTenantInfo(this IServiceCollection services)
    {
        //try add
        services.AddHttpContextAccessor();
        services.TryAddSingleton<ICurrentTenantInfoResolver, DefaultCurrentTenantInfoResolver>();
        return services;
    }
}