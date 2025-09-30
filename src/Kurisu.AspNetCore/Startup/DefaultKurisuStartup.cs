using Kurisu.AspNetCore.Startup.Extensions;
using Kurisu.AspNetCore.UnifyResultAndValidation.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Kurisu.AspNetCore.Startup;

/// <summary>
/// 默认启动类，提供应用程序的基础启动配置。
/// 继承此类可自定义服务注册和请求管道配置。
/// </summary>
[SkipScan]
public abstract class DefaultStartup
{
    /// <summary>
    /// 构造函数，注入全局配置。
    /// </summary>
    /// <param name="configuration">应用程序配置接口</param>
    protected DefaultStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    /// <summary>
    /// 全局配置对象，供子类和方法使用。
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    protected IConfiguration Configuration { get; }

    /// <summary>
    /// 配置启动项，供子类重写以自定义启动参数。
    /// </summary>
    /// <param name="options">启动选项</param>
    protected virtual void ConfigureStartupOptions(StartupOptions options)
    {
        // 可在此处自定义启动选项
    }

    /// <summary>
    /// 配置依赖注入容器，注册服务。
    /// </summary>
    /// <param name="services">服务集合</param>
    public virtual void ConfigureServices(IServiceCollection services)
    {
        // 映射配置文件
        services.AddConfiguration(Configuration);

        // 配置启动项
        this.ConfigureStartupOptions(App.StartupOptions);

        // 注入 HttpContextAccessor
        services.AddHttpContextAccessor();

        // 依赖注入
        services.AddDependencyInjection();

        // 路由配置
        services.AddRouting(App.StartupOptions.RouteOptions);

        // 控制器与 JSON 配置
        services.AddControllers()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";

                options.SerializerSettings.ContractResolver = new DefaultContractResolver()
                {
                    NamingStrategy = new CamelCaseNamingStrategy
                    {
                        OverrideSpecifiedNames = false
                    }
                };
            });
        
        //.ConfigureApplicationPartManager(manager => { manager.FeatureProviders.Add(App.StartupOptions.DynamicApiOptions.ControllerFeatureProvider); });
        // MVC 约定配置
        //services.Configure<MvcOptions>(options => options.Conventions.Add(App.StartupOptions.DynamicApiOptions.ModelConvention));

        // 响应与异常格式统一
        services.AddUnifyResult();

        // 对象关系映射
        services.AddObjectMapper();

        // 注入自定义 pack
        services.AddAppPacks(Configuration);
    }

    /// <summary>
    /// 配置请求处理管道。
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <param name="env">主机环境</param>
    public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // 设置根服务提供器，应用程序唯一
        InternalApp.RootServices = app.ApplicationServices;

        // 创建作用域，初始化 pack
        using (var scope = app.ApplicationServices.CreateScope())
        {
            app.UseAppPacks(env, scope.ServiceProvider, true);
            app.UseRouting();
            app.UseAppPacks(env, scope.ServiceProvider, false);
        }

        // 映射控制器路由
        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
}