using System.Linq;
using System.Reflection;
using Mapster;
using MapsterMapper;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 对象关系映射扩展类
    /// </summary>
    public static class ObjectMapperServiceCollectionExtensions
    {
        /// <summary>
        /// 添加对象关系映射
        /// </summary>
        /// <param name="services">服务容器</param>
        /// <param name="compile">提前编译</param>
        /// <param name="assemblies">扫描程序集</param>
        /// <returns></returns>
        public static IServiceCollection AddKurisuObjectMapper(this IServiceCollection services, bool compile, params Assembly[] assemblies)
        {
            var globalSettings = TypeAdapterConfig.GlobalSettings;

            //扫描IRegister
            if (assemblies != null && assemblies.Any())
                globalSettings.Scan(assemblies);

            //名称匹配规则
            globalSettings.Default.NameMatchingStrategy(NameMatchingStrategy.Flexible);

            //globalSettings 一定要单例注入
            services.AddSingleton(globalSettings);
            services.AddScoped<IMapper, ServiceMapper>();

            //提前编译会增加内容
            if (compile)
            {
                globalSettings.Compile();
            }

            return services;
        }
    }
}