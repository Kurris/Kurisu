using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Kurisu.Startup
{
    /// <summary>
    /// 默认启动类
    /// </summary>
    [SkipScan]
    public abstract class DefaultKurisuStartup : BasePack
    {
        private readonly string _cors = "defaultCors";

        public DefaultKurisuStartup(IConfiguration configuration)
        {
            App.Configuration = configuration;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            //处理api响应时,循环序列化问题
            //返回json为驼峰命名
            services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            });

            //格式统一
            services.AddUnify();

            //映射配置文件 
            services.AddKurisuConfiguration();

            //添加跨域支持
            services.AddCors(options =>
            {
                options.AddPolicy(_cors, builder =>
                {
                    builder.SetIsOriginAllowed(_ => true)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            //添加对象关系映射,扫描程序集
            services.AddKurisuObjectMapper(Assembly.GetExecutingAssembly());

            //依赖注入
            services.AddKurisuDependencyInjection();

            services.AddKurisuDatabaseAccessor();

            foreach (var pack in App.Packs)
            {
                pack.ConfigureServices(services);
            }
        }

        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors(_cors);

            foreach (var pack in App.Packs)
            {
                pack.ServiceProvider = ServiceProvider;
                pack.Configure(app, env);
            }

            app.UseRouting();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}