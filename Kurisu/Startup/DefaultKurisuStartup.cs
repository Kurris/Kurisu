using Kurisu.Authentication.Abstractions;
using Kurisu.Authentication.Internal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Kurisu.Startup
{
    /// <summary>
    /// 默认启动类
    /// </summary>
    [SkipScan]
    public abstract class DefaultKurisuStartup
    {
        protected DefaultKurisuStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }


        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddSingleton<ICurrentTenantInfoResolver, DefaultCurrentTenantInfoResolver>();
            //映射配置文件 
            services.AddKurisuConfiguration(Configuration);

            //处理api响应时,循环序列化问题
            //返回json为驼峰命名
            services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            });

            //依赖注入
            services.AddKurisuDependencyInjection();

            //格式统一
            services.AddKurisuUnifyResult();

            //注入自定义pack
            services.AddKurisuAppPacks(Configuration);
        }

        public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            App.ServiceProvider = app.ApplicationServices;

            app.UseKurisuAppPacks(env, app.ApplicationServices, true);
            app.UseRouting();
            app.UseKurisuAppPacks(env, app.ApplicationServices, false);
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}