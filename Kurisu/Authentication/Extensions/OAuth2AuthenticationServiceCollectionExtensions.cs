using System;
using System.Linq;
using Kurisu.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 授权扩展
    /// </summary>
    public static class OAuth2AuthenticationServiceCollectionExtensions
    {
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

                    //person access token
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
                return (string.Empty, string.Empty);

            var schemeName = bearerValues[0];
            var accessToken = bearerValues[1];

            return (schemeName, accessToken);
        }
    }
}