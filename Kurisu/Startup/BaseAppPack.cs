using System;
using Kurisu.Startup.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Startup
{
    /// <summary>
    /// appPack
    /// </summary>
    public abstract class BaseAppPack : IAppPack
    {
        /// <summary>
        /// 配置
        /// </summary>
        public IConfiguration Configuration { get; set; }

        /// <summary>
        /// 启动顺序
        /// </summary>
        public virtual int Order => 0;

        /// <summary>
        /// 是否启用
        /// </summary>
        public virtual bool IsEnable => true;

        /// <summary>
        /// 在Startup Configure 请求管道中,以UseRouting依据添加自定义AppPack
        /// </summary>
        public abstract bool IsBeforeUseRouting { get; }

        public virtual void Invoke(IServiceProvider serviceProvider)
        {
        }

        public virtual void ConfigureServices(IServiceCollection services)
        {
        }

        public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}