using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.DependencyInjection.Internal
{
    [SkipScan]
    public class NamedResolver : INamedResolver
    {
        private readonly IServiceProvider _serviceProvider;

        public NamedResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }


        /// <summary>
        /// 解析服务名称
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static string GetServiceName(Type type)
        {
            return type.IsDefined(typeof(RegisterAttribute)) ? type.GetCustomAttribute<RegisterAttribute>().Name : type.Name;
        }

        public object GetService(Type type, string serviceName)
        {
            return _serviceProvider.GetServices(type).FirstOrDefault(x => GetServiceName(x.GetType()).Equals(serviceName));
        }

        public TService GetService<TService>(string serviceName) where TService : class
        {
            return _serviceProvider.GetServices(typeof(TService)).FirstOrDefault(x => GetServiceName(x.GetType()).Equals(serviceName)) as TService;
        }

        public TService GetService<TLifeTime, TService>(string serviceName) where TLifeTime : IDependency where TService : class
        {
            return (_serviceProvider.GetService(typeof(Func<string, TLifeTime, object>)) as Func<string, TLifeTime, object>)?.Invoke(serviceName, default) as TService;
        }
    }
}