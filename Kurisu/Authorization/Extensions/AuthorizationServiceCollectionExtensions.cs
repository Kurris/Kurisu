using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Kurisu.Authorization.Extensions
{
    /// <summary>
    /// 授权扩展
    /// </summary>
    public static class AuthorizationServiceCollectionExtensions
    {
        /// <summary>
        /// 添加jwt鉴权
        /// </summary>
        /// <param name="services"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns></returns>
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
        {
            var jwtAppSetting = App.GetConfig<JwtAppSetting>();
            if (jwtAppSetting == null) throw new ArgumentNullException(nameof(JwtAppSetting));

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Events = new JwtBearerEvents()
                    {
                        OnMessageReceived = context =>
                        {
                            var loginProvider = "WebApi";
                            if (!context.Request.Path.StartsWithSegments("/api")) loginProvider = "Signalr";
                            var tokenName = jwtAppSetting.TokenName;

                            context.Token = loginProvider switch
                            {
                                "WebApi" => context.Request.Headers[tokenName],
                                "Signalr" => context.Request.Query[tokenName],
                                _ => throw new NotSupportedException(loginProvider)
                            };
                            return Task.CompletedTask;
                        }
                    };
                    options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtAppSetting.Issuer,
                        ValidAudience = jwtAppSetting.Audience,
                        IssuerSigningKey =
                            new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(jwtAppSetting.TokenSecretKey)),
                        RequireExpirationTime = true,
                    };
                });

            return services;
        }
    }
}