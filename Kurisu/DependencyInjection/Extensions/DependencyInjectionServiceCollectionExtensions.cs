using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;
using Kurisu;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
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
        private static readonly ConcurrentDictionary<string, Type> TypeNamedCollection;

        /// <summary>
        /// 静态初始化
        /// </summary>
        static DependencyInjectionServiceCollectionExtensions()
        {
            //命名服务容器
            TypeNamedCollection = new ConcurrentDictionary<string, Type>();
        }

        /// <summary>
        /// 添加依赖注入，扫描全局
        /// </summary>
        /// <param name="services">服务容器</param>
        /// <returns><see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddKurisuDependencyInjection(this IServiceCollection services)
        {
            //注册服务拦截器
            services.InterceptorRegister();

            //注册服务
            services.ServiceRegister();

            //注册命名服务
            services.AddNamedResolver();
            services.NamedRegister();

            return services;
        }

        /// <summary>
        /// 注册服务
        /// </summary>
        /// <param name="services"></param>
        /// <exception cref="ApplicationException"></exception>
        private static void ServiceRegister(this IServiceCollection services)
        {
            //接口依赖注入类型
            //生命周期类型
            var lifeTimeTypes = new[] {typeof(ITransientDependency), typeof(IScopeDependency), typeof(ISingletonDependency)};

            var serviceTypes = App.ActiveTypes
                .Where(x => x.IsAssignableTo(typeof(IDependency))
                            && !x.IsAssignableTo(typeof(IAsyncInterceptor))
                            && !x.IsAssignableTo(typeof(IInterceptor))
                            && x.IsClass
                            && x.IsPublic
                            && !x.IsAbstract
                            && !x.IsInterface);

            foreach (var service in serviceTypes)
            {
                //获取服务的所有接口
                var interfaces = service.GetInterfaces();

                //获取注册的生命周期类型,不允许多个依赖注入接口
                var lifeTimeInterfaces = interfaces.Where(x => lifeTimeTypes.Contains(x)).Distinct();
                if (lifeTimeInterfaces.Count() > 1) throw new ApplicationException($"{service.FullName}存在多个生命周期定义");

                //具体的生命周期类型
                var currentLifeTimeInterface = lifeTimeInterfaces.First();

                //能够注册的接口
                var ableRegisterInterfaces = interfaces.Where(x => !lifeTimeInterfaces.Contains(x) && x != typeof(IDependency));

                //获取特性RegisterAttribute
                if (service.IsDefined(typeof(RegisterAttribute), false))
                {
                    var registerAttribute = service.GetCustomAttribute<RegisterAttribute>();
                    var typeNamed = registerAttribute?.Name;
                    if (!service.IsGenericType && !string.IsNullOrEmpty(typeNamed))
                    {
                        //缓存类型注册,重复key异常抛出
                        TypeNamedCollection.TryAdd(typeNamed, service);
                    }

                    if (registerAttribute.Interceptors?.Any() == true)
                    {
                        //注册服务
                        RegisterService(services, currentLifeTimeInterface, service, ableRegisterInterfaces, registerAttribute.Interceptors);
                        continue;
                    }
                }

                //注册服务
                RegisterService(services, currentLifeTimeInterface, service, ableRegisterInterfaces);
            }
        }

        /// <summary>
        /// 注册服务拦截器
        /// </summary>
        /// <param name="services"></param>
        private static void InterceptorRegister(this IServiceCollection services)
        {
            var serviceTypes = App.ActiveTypes
                .Where(x => (x.IsAssignableTo(typeof(IAsyncInterceptor)) || x.IsAssignableTo(typeof(IInterceptor)))
                            && x.IsClass
                            && x.IsPublic
                            && !x.IsAbstract
                            && !x.IsInterface);

            //仅注册自己
            foreach (var service in serviceTypes)
            {
                RegisterService(services, typeof(ISingletonDependency), service);
            }
        }

        /// <summary>
        /// 注册命名服务
        /// </summary>
        /// <param name="services"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private static void NamedRegister(this IServiceCollection services)
        {
            var dic = new Dictionary<Type, Type>
            {
                [typeof(ITransientDependency)] = typeof(Func<string, ITransientDependency, object>),
                [typeof(IScopeDependency)] = typeof(Func<string, IScopeDependency, object>),
                [typeof(ISingletonDependency)] = typeof(Func<string, ISingletonDependency, object>),
            };

            foreach (var entry in dic)
            {
                var registerType = GetRegisterType(entry.Key);
                var life = registerType.ToString();
                var addFactory = typeof(ServiceCollectionServiceExtensions).GetMethod("Add" + life, new[] {typeof(IServiceCollection), typeof(Type), typeof(Func<IServiceProvider, object>)});

                addFactory.Invoke(null, new object[]
                {
                    services, entry.Value, (Func<IServiceProvider, object>) (provider =>
                    {
                        return (Func<string, IDependency, object>) ((named, _) =>
                        {
                            var isRegister = TypeNamedCollection.TryGetValue(named, out var service);
                            return isRegister
                                ? provider.GetService(service)
                                : null;
                        });
                    })
                });
            }
        }


        /// <summary>
        /// 注册服务
        /// </summary>
        /// <param name="services">服务容器</param>
        /// <param name="lifeTimeType">生命周期类型</param>
        /// <param name="service">服务类型</param>
        /// <param name="ableRegisterInterfaces">可注册接口</param>
        /// <param name="interceptors"></param>
        private static void RegisterService(IServiceCollection services, Type lifeTimeType, Type service, IEnumerable<Type> ableRegisterInterfaces = null, Type[] interceptors = null)
        {
            //注册自己
            Register(services, lifeTimeType, service);

            //没有可注册的接口
            if (ableRegisterInterfaces == null || !ableRegisterInterfaces.Any())
            {
                return;
            }

            //注册所有接口
            foreach (var @interface in ableRegisterInterfaces)
                Register(services, lifeTimeType, service, @interface, interceptors);
        }


        /// <summary>
        /// 注册服务
        /// </summary>
        /// <param name="services"></param>
        /// <param name="lifeTimeType"></param>
        /// <param name="service"></param>
        /// <param name="interface"></param>
        /// <param name="interceptors"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private static void Register(IServiceCollection services, Type lifeTimeType, Type service, Type @interface = null, Type[] interceptors = null)
        {
            if (service.IsGenericType)
                service = service.Assembly.GetType(service.Namespace! + "." + service.Name);

            if (@interface != null && @interface.IsGenericType)
                @interface = @interface.Assembly.GetType(@interface.Namespace! + "." + @interface.Name);

            var registerType = GetRegisterType(lifeTimeType);
            var life = registerType.ToString();

            var addSelf = typeof(ServiceCollectionServiceExtensions).GetMethod("Add" + life, new[] {typeof(IServiceCollection), typeof(Type)});
            var addSelfWithInterface = typeof(ServiceCollectionServiceExtensions).GetMethod("Add" + life, new[] {typeof(IServiceCollection), typeof(Type), typeof(Type)});
            var addFactory = typeof(ServiceCollectionServiceExtensions).GetMethod("Add" + life, new[] {typeof(IServiceCollection), typeof(Type), typeof(Func<IServiceProvider, object>)});

            if (@interface == null)
                addSelf.Invoke(null, new object[] {services, service});
            else
            {
                if (interceptors?.Any() == true)
                {
                    addFactory.Invoke(null, new object[]
                    {
                        services, @interface, (Func<IServiceProvider, object>) (provider =>
                        {
                            var currService = provider.GetService(service);
                            var currInterceptors = interceptors.Select(x =>
                            {
                                var i = provider.GetService(x);
                                return i switch
                                {
                                    IAsyncInterceptor asyncInterceptor => asyncInterceptor.ToInterceptor(),
                                    IInterceptor interceptor => interceptor,
                                    _ => null
                                };
                            }).Where(x => x != null).ToArray();
                            return new ProxyGenerator().CreateInterfaceProxyWithTargetInterface(@interface, currService, currInterceptors);
                        })
                    });
                }
                else
                {
                    addSelfWithInterface.Invoke(null, new object[] {services, @interface, service});
                }
            }
        }


        /*========================================= help methods ====================================================*/

        /// <summary>
        /// 判断生命周期
        /// </summary>
        /// <param name="lifeTimeType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidCastException"></exception>
        private static ServiceLifetime GetRegisterType(Type lifeTimeType)
        {
            //判断注册方式
            return lifeTimeType.Name switch
            {
                nameof(ITransientDependency) => ServiceLifetime.Transient,
                nameof(ISingletonDependency) => ServiceLifetime.Singleton,
                nameof(IScopeDependency) => ServiceLifetime.Scoped,
                _ => throw new InvalidCastException($"非法生命周期类型{lifeTimeType.Name}")
            };
        }
    }
}