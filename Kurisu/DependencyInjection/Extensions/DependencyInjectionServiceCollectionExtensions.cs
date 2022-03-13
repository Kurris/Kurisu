using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kurisu.DependencyInjection.Abstractions;
using Kurisu.DependencyInjection.Attributes;
using Kurisu.DependencyInjection.Enums;
using Kurisu.Proxy.Attributes;
using Kurisu.Proxy.Global;
using Microsoft.Extensions.DependencyInjection;
using DispatchProxy = Kurisu.Proxy.DispatchProxy;

namespace Kurisu.DependencyInjection.Extensions
{
    /// <summary>
    /// 依赖注入扩展类
    /// </summary>
    [SkipScan]
    public static class DependencyInjectionServiceCollectionExtensions
    {
        /// <summary>
        /// 类型名称集合,name->registerService
        /// </summary>
        private static readonly ConcurrentDictionary<string, Type> _typeNamedCollection;

        /// <summary>
        /// 创建代理的方法
        /// </summary>
        private static readonly MethodInfo _dispatchCreateMethod;

        /// <summary>
        /// 全局服务代理类型
        /// </summary>
        private static readonly Type _globalServiceProxyType;

        /// <summary>
        /// 静态初始化
        /// </summary>
        static DependencyInjectionServiceCollectionExtensions()
        {
            //命名服务容器
            _typeNamedCollection = new ConcurrentDictionary<string, Type>();

            //代理创建方法
            _dispatchCreateMethod = typeof(DispatchProxy).GetMethod(nameof(DispatchProxy.Create), BindingFlags.Static | BindingFlags.NonPublic);

            //全局代理类型
            _globalServiceProxyType = App.ActiveTypes.FirstOrDefault(u => typeof(DispatchProxy).IsAssignableFrom(u)
                                                                          && typeof(IGlobalDispatchProxy).IsAssignableFrom(u)
                                                                          && u.IsClass
                                                                          && !u.IsInterface
                                                                          && !u.IsAbstract);
        }

        /// <summary>
        /// 添加依赖注入，扫描全局
        /// </summary>
        /// <param name="services">服务容器</param>
        /// <returns><see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddDependencyInjectionService(this IServiceCollection services)
        {
            //生命周期类型
            var lifeTimeTypes = new[] { typeof(ITransientDependency), typeof(IScopeDependency), typeof(ISingletonDependency) };

            //可注册类型
            var serviceTypes = App.ActiveTypes
                .Where(x => x.IsAssignableTo(typeof(IDependency))
                            && x.IsClass
                            && x.IsPublic
                            && !x.IsAbstract
                            && !x.IsInterface);

            //注册依赖注入
            foreach (var service in serviceTypes)
            {
                // 服务特性注册方式
                var registerAttribute = service.IsDefined(typeof(RegisterAttribute), false)
                    ? service.GetCustomAttribute<RegisterAttribute>()
                    : new RegisterAttribute();

                //获取服务的所有接口
                var interfaces = service.GetInterfaces();

                //获取注册的生命周期类型,不允许多个依赖注入接口
                var lifeTimeInterfaces = interfaces.Where(x => lifeTimeTypes.Contains(x)).Distinct();
                if (lifeTimeInterfaces.Count() > 1) throw new ApplicationException($"{service.FullName}存在多个生命周期依赖");

                //具体的生命周期类型
                var currentLifeTimeInterface = lifeTimeInterfaces.First();

                //能够注册的接口
                var ableInterfaces = interfaces.Where(x => !lifeTimeInterfaces.Contains(x)
                                                           && x != typeof(IDependency));

                var typeNamed = registerAttribute?.Named ?? service.Name;
                if (!service.IsGenericType)
                {
                    //缓存类型注册
                    _typeNamedCollection.TryAdd(typeNamed, service);
                }

                //注册服务
                RegisterService(services, currentLifeTimeInterface, service, registerAttribute, ableInterfaces);
            }

            //注册命名服务
            RegisterNamedService<ITransientDependency>(services);
            RegisterNamedService<IScopeDependency>(services);
            RegisterNamedService<ISingletonDependency>(services);

            return services;
        }


        /// <summary>
        /// 命名注册服务
        /// </summary>
        /// <param name="services"></param>
        /// <typeparam name="TLifeTimeType"></typeparam>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private static void RegisterNamedService<TLifeTimeType>(this IServiceCollection services) where TLifeTimeType : IDependency
        {
            var registerType = GetRegisterType(typeof(TLifeTimeType));
            switch (registerType)
            {
                case RegisterType.Transient:
                    services.AddTransient(provider =>
                    {
                        return (Func<string, IScopeDependency, object>)((named, _) =>
                        {
                            var isRegister = _typeNamedCollection.TryGetValue(named, out var service);
                            return isRegister
                                ? provider.GetService(service)
                                : null;
                        });
                    });
                    break;
                case RegisterType.Scoped:
                    services.AddScoped(provider =>
                    {
                        return (Func<string, IScopeDependency, object>)((named, _) =>
                        {
                            var isRegister = _typeNamedCollection.TryGetValue(named, out var service);
                            return isRegister
                                ? provider.GetService(service)
                                : null;
                        });
                    });
                    break;
                case RegisterType.Singleton:
                    services.AddSingleton(provider =>
                    {
                        return (Func<string, ISingletonDependency, object>)((named, _) =>
                        {
                            var isRegister = _typeNamedCollection.TryGetValue(named, out var service);
                            return isRegister
                                ? provider.GetService(service)
                                : null;
                        });
                    });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        /// <summary>
        /// 注册服务
        /// </summary>
        /// <param name="services">服务容器</param>
        /// <param name="lifeTimeType">生命周期类型</param>
        /// <param name="service">服务类型</param>
        /// <param name="registerAttribute">注册特性</param>
        /// <param name="ableInterfaces">可注册接口</param>
        private static void RegisterService(IServiceCollection services, Type lifeTimeType, Type service,
            RegisterAttribute registerAttribute, IEnumerable<Type> ableInterfaces)
        {
            //注册自己
            Register(services, lifeTimeType, service, registerAttribute);

            //没有可注册的接口
            if (!ableInterfaces.Any()) return;

            //注册所有接口
            foreach (var @interface in ableInterfaces)
                Register(services, lifeTimeType, service, registerAttribute, @interface);
        }


        /// <summary>
        /// 注册服务
        /// </summary>
        /// <param name="services"></param>
        /// <param name="lifeTimeType"></param>
        /// <param name="service"></param>
        /// <param name="registerAttribute"></param>
        /// <param name="interface"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private static void Register(IServiceCollection services, Type lifeTimeType, Type service,
            RegisterAttribute registerAttribute, Type @interface = null)
        {
            if (service.IsGenericType)
            {
                service = service.MakeGenericType(typeof(object));
            }


            var registerType = GetRegisterType(lifeTimeType);
            switch (registerType)
            {
                case RegisterType.Transient:
                    if (@interface == null)
                        services.AddTransient(service);
                    else
                    {
                        services.AddTransient(@interface, service);
                        AddDispatchProxy(services, lifeTimeType, service, registerAttribute.Proxy, @interface);
                    }

                    break;
                case RegisterType.Scoped:
                    if (@interface == null)
                        services.AddScoped(service);
                    else
                    {
                        services.AddScoped(@interface, service);
                        AddDispatchProxy(services, lifeTimeType, service, registerAttribute.Proxy, @interface);
                    }

                    break;
                case RegisterType.Singleton:
                    if (@interface == null)
                        services.AddSingleton(service);
                    else
                    {
                        services.AddSingleton(@interface, service);
                        AddDispatchProxy(services, lifeTimeType, service, registerAttribute.Proxy, @interface);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        /// <summary>
        /// 判断生命周期
        /// </summary>
        /// <param name="lifeTimeType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidCastException"></exception>
        private static RegisterType GetRegisterType(Type lifeTimeType)
        {
            //判断注册方式
            return lifeTimeType.Name switch
            {
                nameof(ITransientDependency) => RegisterType.Transient,
                nameof(ISingletonDependency) => RegisterType.Singleton,
                nameof(IScopeDependency) => RegisterType.Scoped,
                _ => throw new InvalidCastException($"非法生命周期类型{lifeTimeType.Name}")
            };
        }


        /// <summary>
        /// 创建服务代理
        /// </summary>
        /// <param name="services"></param>
        /// <param name="lifeTimeType"></param>
        /// <param name="service"></param>
        /// <param name="proxyType"></param>
        /// <param name="interface"></param>
        private static void AddDispatchProxy(IServiceCollection services, Type lifeTimeType, Type service,
            Type proxyType, Type @interface)
        {
            proxyType ??= _globalServiceProxyType;
            if (proxyType == null
                || service != null && service.IsDefined(typeof(SuppressProxyAttribute), true) //服务
                || proxyType.IsDefined(typeof(SuppressProxyAttribute), true)) return; //aop拦截器 

            var registerType = GetRegisterType(lifeTimeType);

            //替换接口原有的注入类型
            switch (registerType)
            {
                case RegisterType.Transient:
                    services.AddTransient(@interface, AopFactory);
                    break;
                case RegisterType.Scoped:
                    services.AddScoped(@interface, AopFactory);
                    break;
                case RegisterType.Singleton:
                    services.AddTransient(@interface, AopFactory);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            //aop method factory
            DispatchProxy AopFactory(IServiceProvider serviceProvider)
            {
                var proxy = _dispatchCreateMethod.MakeGenericMethod(@interface, proxyType).Invoke(null, null) as DispatchProxy;

                proxy.ServiceProvider = serviceProvider;
                if (service != null) proxy.Target = serviceProvider.GetService(service);

                //属性注入
                foreach (var propertyInfo in proxy.GetType().GetProperties().Where(x => x.IsDefined(typeof(RegisterAttribute))))
                {
                    propertyInfo.SetValue(proxy, serviceProvider.GetService(propertyInfo.PropertyType));
                }

                return proxy;
            }
        }
    }
}