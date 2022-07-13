using System;
using Kurisu.Startup.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Startup
{
    /// <summary>
    /// 基础Pack
    /// </summary>
    public abstract class BasePack : IKurisuPack
    {
        public int Order => 1;

        public bool IsEnable { get; set; } = true;

        public IServiceProvider ServiceProvider { get; set; }


        public virtual void Invoke()
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