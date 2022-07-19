using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Startup.Abstractions
{
    public interface IAppPack : IOrderedFilter
    {
        /// <summary>
        /// 触发方法
        /// </summary>
        void Invoke(IServiceProvider serviceProvider);

        /// <summary>
        /// 添加服务
        /// </summary>
        /// <param name="services"></param>
        void ConfigureServices(IServiceCollection services);

        /// <summary>
        /// 配置管道服务
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        void Configure(IApplicationBuilder app, IWebHostEnvironment env);
    }
}