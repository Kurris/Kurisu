using System;
using System.Linq;
using Kurisu;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class AppPackExtensions
    {
        /// <summary>
        /// 添加自定义appPacks
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddKurisuAppPacks(this IServiceCollection services)
        {
            foreach (var appPack in App.AppPacks)
            {
                appPack.ConfigureServices(services);
            }

            return services;
        }

        /// <summary>
        /// 使用自定义appPacks
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <param name="serviceProvider"></param>
        /// <param name="isBeforeUseRouting"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseKurisuAppPacks(this IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider, bool isBeforeUseRouting)
        {
            foreach (var appPack in App.AppPacks.Where(x => x.IsBeforeUseRouting == isBeforeUseRouting))
            {
                appPack.Invoke(serviceProvider);
                appPack.Configure(app, env);
            }

            return app;
        }
    }
}