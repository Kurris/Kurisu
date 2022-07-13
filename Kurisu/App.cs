using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using Kurisu.Startup;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kurisu
{
    /// <summary>
    /// 应用程序全局类
    /// </summary>
    public class App
    {
        static App()
        {
            Initialize();
        }

        /// <summary>
        /// 应用程序有效类型
        /// </summary>
        public static IEnumerable<Type> ActiveTypes { get; private set; }

        /// <summary>
        /// 服务提供器
        /// </summary>
        public static IServiceProvider ServiceProvider { get; set; }


        /// <summary>
        /// 全局配置文件
        /// </summary>
        internal static IConfiguration Configuration { get; set; }

        /// <summary>
        /// 请求上下文
        /// </summary>
        public static HttpContext HttpContext => ServiceProvider?.GetService<IHttpContextAccessor>()?.HttpContext;

        /// <summary>
        /// 请求上下文用户
        /// </summary>
        public static ClaimsPrincipal User => HttpContext?.User;


        private static IEnumerable<BasePack> _packs;

        /// <summary>
        /// 自定义处理pack
        /// </summary>
        public static IEnumerable<BasePack> Packs
        {
            get
            {
                if (_packs == null)
                {
                    var packTypes = ActiveTypes.Where(x => x.IsSubclassOf(typeof(BasePack)));
                    _packs = packTypes.Select(x => Activator.CreateInstance(x) as BasePack)
                        .Where(x => x.IsEnable)
                        .OrderBy(x => x.Order);
                }

                return _packs;
            }
        }

        /// <summary>
        /// 从Configuration中直接获取
        /// </summary>
        /// <typeparam name="TOptions">配置类</typeparam>
        /// <returns><see cref="TOptions"/></returns>
        internal static TOptions GetOptions<TOptions>()
            where TOptions : class, new()
        {
            var options = ServiceProvider.GetService<IOptions<TOptions>>();
            return options?.Value;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        private static void Initialize()
        {
            LoadActiveTypes();
        }

        /// <summary>
        /// 加载可用类型
        /// </summary>
        private static void LoadActiveTypes()
        {
            //所有程序集
            var activeAssemblies = new List<Assembly>();

            //添加当前程序集
            activeAssemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies());

            var references = Assembly.GetExecutingAssembly().GetReferencedAssemblies();

            //添加引用的程序集
            foreach (var reference in references)
            {
                if (!activeAssemblies.Exists(x => x.FullName.Equals(reference.FullName, StringComparison.OrdinalIgnoreCase)))
                {
                    var refAssembly = Assembly.Load(reference);
                    activeAssemblies.Add(refAssembly);
                }
            }

            //排除无效type
            ActiveTypes = activeAssemblies.SelectMany(x => x.GetTypes()
                .Where(type => type.IsPublic)
                .Where(type => !type.FullName.StartsWith("System"))
                .Where(type => !type.FullName.StartsWith("Microsoft"))
                .Where(type => !type.FullName.StartsWith("Internal"))
                .Where(type => !type.FullName.StartsWith("Swashbuckle"))
                .Where(type => !type.FullName.StartsWith("Serilog"))
                .Where(type => !type.FullName.StartsWith("Mapster"))
                .Where(type => !type.FullName.StartsWith("Pomelo"))
                .Where(type => !type.FullName.StartsWith("Newtonsoft"))
                .Where(type => !type.FullName.StartsWith("MySql"))
                .Where(type => !type.FullName.StartsWith("Nest"))
                .Where(type => !type.FullName.StartsWith("Elasticsearch"))
                .Where(type => !type.IsDefined(typeof(SkipScanAttribute)))
            );
        }
    }
}