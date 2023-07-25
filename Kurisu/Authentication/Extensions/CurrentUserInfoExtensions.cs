using Kurisu.Authentication.Abstractions;
using Kurisu.Authentication.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 提供获取当前用户的能力
/// </summary>
public static class CurrentUserInfoExtensions
{
    public static IServiceCollection AddKurisuUserInfo(this IServiceCollection services)
    {
        //try add
        services.AddHttpContextAccessor();
        services.TryAddSingleton<ICurrentUserInfoResolver, DefaultCurrentUserInfoResolver>();
        return services;
    }
}