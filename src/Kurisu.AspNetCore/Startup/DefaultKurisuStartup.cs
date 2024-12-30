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
/// 默认启动类
/// </summary>
[SkipScan]
public abstract class DefaultStartup
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="configuration"></param>
    protected DefaultStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    /// <summary>
    /// config
    /// </summary>
    protected IConfiguration Configuration { get; }

    /// <summary>
    /// 配置ioc
    /// </summary>
    /// <param name="services"></param>
    public virtual void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        //映射配置文件
        services.AddConfiguration(Configuration);

        //依赖注入
        services.AddDependencyInjection();

        //路由小写
        services.AddRouting(options => { options.LowercaseUrls = true; });

        //处理api响应时,循环序列化问题
        //返回json为驼峰命名
        //时间格式
        services.AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";

                var contractResolver = new CamelCasePropertyNamesContractResolver
                {
                    NamingStrategy =
                    {
                        //优先使用JsonPropertyAttribute
                        OverrideSpecifiedNames = false
                    }
                };
                options.SerializerSettings.ContractResolver = contractResolver;
            });

        //响应,异常格式统一
        services.AddUnifyResult();

        //对象关系映射
        services.AddObjectMapper();

        //注入自定义pack
        services.AddAppPacks(Configuration);
    }

    /// <summary>
    /// 配置请求管道
    /// </summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        //根服务提供器,应用程序唯一
        InternalApp.RootServices = app.ApplicationServices;

        using (var scope = app.ApplicationServices.CreateScope())
        {
            app.UseAppPacks(env, scope.ServiceProvider, true);
            app.UseRouting();
            app.UseAppPacks(env, scope.ServiceProvider, false);
        }

        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
}