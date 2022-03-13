using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kurisu.Startup.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Startup
{
    public abstract class BasePack : IKurisuPack
    {
        /// <summary>
        /// 是否启用
        /// </summary>
        public virtual bool IsEnable { get; protected set; }

        /// <summary>
        /// 执行顺序
        /// </summary>
        public virtual int Order => 0;


        public virtual void OnRun(IServiceProvider serviceProvider)
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
