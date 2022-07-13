using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Startup.Abstractions
{
    public interface IKurisuPack : IOrderedFilter
    {
        bool IsEnable { get; set; }

        /// <summary>
        /// 服务提供器
        /// </summary>
        IServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// 触发方法
        /// </summary>
        void Invoke();

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