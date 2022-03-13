using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Startup.Abstractions
{
    public interface IKurisuPack
    {
        /// <summary>
        /// 触发动作
        /// </summary>
        /// <param name="serviceProvider"></param>
        void OnRun(IServiceProvider serviceProvider);

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
        /// <param name="serviceProvider"></param>
        void Configure(IApplicationBuilder app, IWebHostEnvironment env);
    }
}
