using System;
using System.Text;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Authentication.Extensions;
using Kurisu.AspNetCore.Authentication.Options;
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
    /// <exception cref="ArgumentNullException"></exception>
    /// <returns></returns>
    public static IServiceCollection AddKurisuJwtAuthentication(this IServiceCollection services, JwtOptions jwtSetting)
    {
        services.AddUserInfo();
        
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = !string.IsNullOrEmpty(jwtSetting.Issuer),
                    ValidateAudience = !string.IsNullOrEmpty(jwtSetting.Audience),
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSetting.Issuer,
                    ValidAudience = jwtSetting.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSetting.SecretKey)),
                    RequireExpirationTime = true,
                    //ClockSkew
                };
            });

        return services;
    }
}