using System;
using System.Linq;
using Kurisu.ConfigurableOptions.Extensions;
using Kurisu.DependencyInjection.Attributes;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Cors.Extensions
{
    /// <summary>
    /// 跨域中间件扩展
    /// </summary>
    [SkipScan]
    public static class CorsServiceCollectionExtensions
    {
        /// <summary>
        /// 添加跨域中间件
        /// </summary>
        /// <param name="services"></param>
        /// <param name="setupAction"></param>
        /// <returns></returns>
        public static IServiceCollection AddCorsPolicy(this IServiceCollection services,
            Action<CorsOptions> setupAction = null)
        {
            //注入cors配置
            services.AddConfigurableOptions<CorsAppSetting>();

            var corsAppSetting = App.GetConfig<CorsAppSetting>(loadPostConfigure: true);

            //添加跨域中间件
            services.AddCors(options =>
            {
                options.AddPolicy(corsAppSetting.PolicyName, builder =>
                {
                    //hasOrigins 用于判断Origins和Credentials
                    var hasOrigins = corsAppSetting.WithOrigins == null || !corsAppSetting.WithOrigins.Any();
                    if (!hasOrigins) builder.AllowAnyOrigin();
                    else
                        builder.WithOrigins(corsAppSetting.WithOrigins)
                            .SetIsOriginAllowedToAllowWildcardSubdomains();

                    // 配置跨域凭据
                    if (corsAppSetting.AllowCredentials == true && !hasOrigins) builder.AllowCredentials();

                    // 设置预检过期时间
                    if (corsAppSetting.SetPreflightMaxAge.HasValue)
                        builder.SetPreflightMaxAge(TimeSpan.FromSeconds(corsAppSetting.SetPreflightMaxAge.Value));
                });

                setupAction?.Invoke(options);
            });

            return services;
        }
    }
}