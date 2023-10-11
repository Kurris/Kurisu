using System;
using System.Linq;
using Kurisu.Authentication.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 授权扩展
/// </summary>
[SkipScan]
public static class OAuth2AuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// 添加 identity server 鉴权
    /// </summary>
    /// <param name="services"></param>
    /// <param name="setting"></param>
    /// <returns></returns>
    public static IServiceCollection AddKurisuOAuth2Authentication(this IServiceCollection services, IdentityServerSetting setting)
    {
        if (setting == null)
            return services;

        var builder = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = setting.Authority;
                //options.MetadataAddress = "http://localhost:5000/.well-known/openid-configuration";
                options.RequireHttpsMetadata = setting.RequireHttpsMetadata;

                options.TokenValidationParameters.ValidateIssuer = !string.IsNullOrEmpty(setting.Issuer);
                options.TokenValidationParameters.ValidIssuer = setting.Issuer;

                options.TokenValidationParameters.ValidateAudience = !string.IsNullOrEmpty(setting.Audience);
                options.TokenValidationParameters.ValidAudience = setting.Audience;

                options.TokenValidationParameters.ValidTypes = new[] { "at+jwt" };

                //person access token
                if (setting.Pat.Enable)
                {
                    options.ForwardDefaultSelector = context =>
                    {
                        var (scheme, token) = GetBearerValueTuple(context);
                        //如果不是bearer则转到pat的scheme进行验证
                        return scheme.Equals(JwtBearerDefaults.AuthenticationScheme, StringComparison.OrdinalIgnoreCase) && !token.Contains('.') ? setting.Pat.Scheme : null;
                    };
                }
            });

        if (setting.Pat.Enable)
        {
            builder.AddOAuth2Introspection(setting.Pat.Scheme, options =>
            {
                options.DiscoveryPolicy.RequireHttps = setting.RequireHttpsMetadata;
                options.DiscoveryPolicy.ValidateIssuerName = !string.IsNullOrEmpty(setting.Issuer);
                options.ClaimsIssuer = setting.Issuer;
                options.Authority = setting.Authority;
                options.ClientId = setting.Pat.ClientId;
                options.ClientSecret = setting.Pat.ClientSecret;
            });
        }

        return services;
    }


    /// <summary>
    /// 获取Authorization=Bearer value
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    private static (string scheme, string token) GetBearerValueTuple(HttpContext context)
    {
        var authorizationValue = context.Request.Headers[HeaderNames.Authorization].FirstOrDefault();

        if (string.IsNullOrEmpty(authorizationValue))
        {
            return (string.Empty, string.Empty);
        }

        var bearerValues = authorizationValue.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (bearerValues.Length != 2)
            return (string.Empty, string.Empty);

        var schemeName = bearerValues[0];
        var accessToken = bearerValues[1];

        return (schemeName, accessToken);
    }
}