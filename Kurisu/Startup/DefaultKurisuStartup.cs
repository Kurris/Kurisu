using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Kurisu.Startup;

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
        //映射配置文件
        services.AddKurisuConfiguration(Configuration);

        //依赖注入
        services.AddKurisuDependencyInjection();

        //路由小写
        services.AddRouting(options => { options.LowercaseUrls = true; });

        //处理api响应时,循环序列化问题
        //返回json为驼峰命名
        //时间格式
        services.AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true).AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
            options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        });

        //响应,异常格式统一
        services.AddKurisuUnifyResult();

        //注入自定义pack
        services.AddKurisuAppPacks(Configuration);
    }

    public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        //根服务提供器,应用程序唯一
        InternalApp.ApplicationServices = app.ApplicationServices;

        //处理请求中产生的自定义范围对象
        app.Use(async (_, next) =>
        {
            //默认的管道执行
            await next();

            //释放当前请求作用域申请的IServiceProvider
            App.DisposeObjects();
        });

        using (var scope = app.ApplicationServices.CreateScope())
        {
            app.UseKurisuAppPacks(env, scope.ServiceProvider, true);
            app.UseRouting();
            app.UseKurisuAppPacks(env, scope.ServiceProvider, false);
        }

        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
}