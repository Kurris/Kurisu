﻿using Kurisu.Authentication.Abstractions;
using Kurisu.Authentication.Defaults;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddKurisuUserInfo(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton<ICurrentUserInfo, DefaultCurrentUserInfoResolver>();
        return services;
    }

    public static IServiceCollection AddKurisuTenantInfo(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton<ICurrentTenantInfo, DefaultCurrentTenantInfoResolver>();
        return services;
    }
}
