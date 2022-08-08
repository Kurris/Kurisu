using System.Reflection;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.Abstractions;
using Kurisu.DataAccessor.Functions.UnitOfWork.Abstractions;
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
    public abstract class DefaultKurisuStartup : BaseAppPack
    {
        protected DefaultKurisuStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public override bool IsBeforeUseRouting { get; }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();

            //处理api响应时,循环序列化问题
            //返回json为驼峰命名
            services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            });

            //格式统一
            services.AddKurisuUnifyResult();

            //映射配置文件 
            services.AddKurisuConfiguration(Configuration);

            //添加对象关系映射,扫描程序集
            services.AddKurisuObjectMapper(false, Assembly.GetEntryAssembly());

            //依赖注入
            services.AddKurisuDependencyInjection();

            //注入数据访问
            services.AddKurisuDatabaseAccessor(null)
                .AddKurisuUnitOfWork(provider => provider.GetService<IAppMasterDb>().GetMasterDbContext() as IUnitOfWorkDbContext);

            //注入自定义pack
            services.AddKurisuAppPacks(Configuration);
        }

        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            App.ServiceProvider = app.ApplicationServices;

            app.UseKurisuAppPacks(env, app.ApplicationServices, true);
            app.UseRouting();
            app.UseKurisuAppPacks(env, app.ApplicationServices, false);
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}