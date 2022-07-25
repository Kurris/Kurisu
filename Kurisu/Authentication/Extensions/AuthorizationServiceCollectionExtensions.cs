using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kurisu.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
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
        /// <param name="jwtSetting"></param>
        /// <param name="onMessageReceived"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns></returns>
        public static IServiceCollection AddKurisuJwtAuthentication(this IServiceCollection services, JwtSetting jwtSetting, Action<MessageReceivedContext> onMessageReceived)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            onMessageReceived?.Invoke(context);
                            return Task.CompletedTask;
                        }
                    };
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


        /// <summary>
        /// 添加 identit server 鉴权
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddKurisuOAuth2Authentication(this IServiceCollection services)
        {
            var setting = services.AddKurisuOptions<IdentityServerSetting>();

            var builder = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = setting.Authority;
                    options.RequireHttpsMetadata = setting.RequireHttpsMetadata;

                    options.TokenValidationParameters.ValidateIssuer = !string.IsNullOrEmpty(setting.Issuer);
                    options.TokenValidationParameters.ValidIssuer = setting.Issuer;

                    options.TokenValidationParameters.ValidateAudience = !string.IsNullOrEmpty(setting.Audience);
                    options.TokenValidationParameters.ValidAudience = setting.Audience;

                    options.TokenValidationParameters.ValidTypes = new[] {"at+jwt"};

                    if (setting.Pat.Enable)
                    {
                        options.ForwardDefaultSelector = context =>
                        {
                            (string scheme, string token) = GetBearerValueTuple(context);
                            return scheme.Equals(JwtBearerDefaults.AuthenticationScheme, StringComparison.OrdinalIgnoreCase) && !token.Contains(".") ? "token" : null;
                        };
                    }
                });

            if (setting.Pat.Enable)
            {
                builder.AddOAuth2Introspection("token", options =>
                {
                    options.DiscoveryPolicy.RequireHttps = true;
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
            string authorizationValue = context.Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(authorizationValue))
            {
                return (string.Empty, string.Empty);
            }

            string[] bearerValues = authorizationValue.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (bearerValues.Length != 2)
            {
                return (string.Empty, string.Empty);
            }

            var schemeName = bearerValues[0];
            var accessToken = bearerValues[1];

            return (schemeName, accessToken);
        }
    }
}