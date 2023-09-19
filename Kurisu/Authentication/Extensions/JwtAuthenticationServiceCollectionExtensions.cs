using System;
using System.Text;
using Kurisu.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 授权扩展
/// </summary>
public static class JwtAuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// 添加jwt鉴权
    /// </summary>
    /// <param name="services"></param>
    /// <param name="jwtSetting"></param>
    /// <param name="onMessageReceived"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <returns></returns>
    public static IServiceCollection AddKurisuJwtAuthentication(this IServiceCollection services, JwtSetting jwtSetting, Action<MessageReceivedContext> onMessageReceived)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                //     options.Events = new JwtBearerEvents
                //     {
                //         OnMessageReceived = context =>
                //         {
                //             onMessageReceived?.Invoke(context);
                //             return Task.CompletedTask;
                //         }
                //     };
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = !string.IsNullOrEmpty(jwtSetting.Issuer),
                    ValidateAudience = !string.IsNullOrEmpty(jwtSetting.Audience),
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSetting.Issuer,
                    ValidAudience = jwtSetting.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSetting.TokenSecretKey)),
                    RequireExpirationTime = true,
                };
            });

        return services;
    }
}