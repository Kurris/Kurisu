using Kurisu.AspNetCore.Abstractions.Authentication;
using Kurisu.AspNetCore.Authentication.Defaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kurisu.AspNetCore.Authentication.Extensions;

/// <summary>
/// 添加用户获取
/// </summary>
public static class AuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// 添加用户获取
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddUserInfo(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton<ICurrentUser, DefaultCurrentUser>();
        services.TryAddSingleton<ICurrentTenant, DefaultCurrentTenant>();
        return services;
    }
}
