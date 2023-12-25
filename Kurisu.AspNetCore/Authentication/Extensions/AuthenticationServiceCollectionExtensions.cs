using Kurisu.AspNetCore.Authentication.Defaults;
using Kurisu.Core.User.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kurisu.AspNetCore.Authentication.Extensions;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddUserInfo(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton<ICurrentUser, DefaultCurrentUser>();
        services.TryAddSingleton<ICurrentTenant, DefaultCurrentTenant>();
        return services;
    }
}
