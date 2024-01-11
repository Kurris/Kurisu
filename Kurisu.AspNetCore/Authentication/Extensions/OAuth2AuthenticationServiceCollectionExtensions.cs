using System;
using System.Linq;
using System.Net.Http;
using Kurisu.AspNetCore.Authentication.Extensions;
using Kurisu.AspNetCore.Authentication.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
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
    /// <param name="options"></param>
    /// <returns></returns>
    public static IServiceCollection AddOAuth2Authentication(this IServiceCollection services, IdentityServerOptions options)
    {
        services.AddUserInfo();
        services.AddHttpClient("AuthenticationBackchannel", (sp, httpClient) =>
            {
                if (sp.GetService<IWebHostEnvironment>().IsProduction())
                {
                    httpClient.DefaultRequestHeaders.Add("X-Requested-Internal", "internal");
                }
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler
                {
                    ClientCertificateOptions = ClientCertificateOption.Manual,
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                };

                return handler;
            });

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IHttpClientFactory>((configureOptions, httpClientFactory) => { configureOptions.Backchannel = httpClientFactory.CreateClient("AuthenticationBackchannel"); });

        var builder = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(configureOptions =>
            {
                configureOptions.Authority = options.Authority;
                configureOptions.RequireHttpsMetadata = options.RequireHttpsMetadata;

                configureOptions.TokenValidationParameters.ValidateIssuer = !string.IsNullOrEmpty(options.Issuer);
                configureOptions.TokenValidationParameters.ValidIssuer = options.Issuer;

                configureOptions.TokenValidationParameters.ValidateAudience = !string.IsNullOrEmpty(options.Audience);
                configureOptions.TokenValidationParameters.ValidAudience = options.Audience;

                configureOptions.TokenValidationParameters.ValidTypes = new[] { "at+jwt" };

                //person access token
                if (options.Pat.Enable)
                {
                    configureOptions.ForwardDefaultSelector = context =>
                    {
                        (string scheme, string token) = GetBearerValueTuple(context);
                        //如果不是bearer则转到pat的scheme进行验证
                        return scheme.Equals(JwtBearerDefaults.AuthenticationScheme, StringComparison.OrdinalIgnoreCase) && !token.Contains('.') ? options.Pat.Scheme : null;
                    };
                }
            });


        if (options.Pat.Enable)
        {
            builder.AddOAuth2Introspection(options.Pat.Scheme, configureOptions =>
            {
                configureOptions.DiscoveryPolicy.RequireHttps = options.RequireHttpsMetadata;
                configureOptions.DiscoveryPolicy.ValidateIssuerName = !string.IsNullOrEmpty(options.Issuer);
                configureOptions.ClaimsIssuer = options.Issuer;
                configureOptions.Authority = options.Authority;
                configureOptions.ClientId = options.Pat.ClientId;
                configureOptions.ClientSecret = options.Pat.ClientSecret;
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