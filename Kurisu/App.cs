using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using Kurisu.ConfigurableOptions.Abstractions;
using Kurisu.DependencyInjection.Attributes;
using Kurisu.Startup;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kurisu
{
    /// <summary>
    /// 应用程序全局类
    /// </summary>
    public static class App
    {
        static App()
        {
            //未托管类型
            _unManagedObjects = new ConcurrentBag<IDisposable>();

            //有效类型
            LoadActiveTypes();
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


        /// <summary>
        /// web主机运行环境
        /// </summary>
        public static IWebHostEnvironment Env { get; set; }

        /// <summary>
        /// 自定义处理pack
        /// </summary>
        public static IEnumerable<BasePack> Packs { get; set; }


        /// <summary>
        /// 未托管的对象集合
        /// </summary>
        private static readonly ConcurrentBag<IDisposable> _unManagedObjects;

        /// <summary>
        /// 获取静态缓存配置，程序初始化使用
        /// </summary>
        /// <typeparam name="TOptions">配置类</typeparam>
        /// <param name="path">配置节点路径,如果为空,取类型名称</param>
        /// <param name="loadPostConfigure">是否设置默认值</param>
        /// <returns><see cref="TOptions"/></returns>
        public static TOptions GetConfig<TOptions>(string path = null, bool loadPostConfigure = false)
            where TOptions : class, new()
        {
            var options = path == null
                ? Configuration.GetSection(typeof(TOptions).Name).Get<TOptions>()
                : Configuration.GetSection(path).Get<TOptions>();

            // 加载默认选项配置
            if (!loadPostConfigure || !typeof(IPostConfigure<TOptions>).IsAssignableFrom(typeof(TOptions)))
                return options;

            if (options is IPostConfigure<TOptions> postConfigure)
            {
                options ??= Activator.CreateInstance<TOptions>();
                postConfigure.PostConfigure(Configuration, options);
            }

            return options;
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
                .Where(x => !x.FullName.StartsWith("System"))
                .Where(x => !x.FullName.StartsWith("Microsoft"))
                .Where(x => !x.FullName.StartsWith("Internal"))
                .Where(x => !x.FullName.StartsWith("Swashbuckle"))
                .Where(x => !x.FullName.StartsWith("Serilog"))
                .Where(x => !x.FullName.StartsWith("Mapster"))
                .Where(x => !x.FullName.StartsWith("Pomelo"))
                .Where(x => !x.FullName.StartsWith("Newtonsoft"))
                .Where(x => !x.FullName.StartsWith("MySql"))
                .Where(type => !type.IsDefined(typeof(SkipScanAttribute), false))
                );

        }

        /// <summary>
        /// 释放未托管的对象
        /// </summary>
        public static void Dispose()
        {
            foreach (var unManagedObject in _unManagedObjects)
            {
                try
                {
                    unManagedObject.Dispose();
                }
                catch
                {

                }
            }

            _unManagedObjects.Clear();
        }
    }
}