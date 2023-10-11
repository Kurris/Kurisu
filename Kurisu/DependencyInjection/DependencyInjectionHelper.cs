using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.DependencyInjection;

internal class DependencyInjectionHelper
{
    //生命周期类型
    public static readonly Type[] LifeTimeTypes = new[] { typeof(ITransientDependency), typeof(IScopeDependency), typeof(ISingletonDependency) };

    public static readonly IEnumerable<Type> Services = App.ActiveTypes.Where(x => x.IsClass
                        && x.IsPublic
                        && !x.IsAbstract
                        && !x.IsInterface);


    public static (ServiceLifetime lifeTime, Type[] interfaceTypes) GetInterfacesAndLifeTime(Type type)
    {
        //获取服务的所有接口
        var interfaces = type.GetInterfaces();

        //获取注册的生命周期类型,不允许多个依赖注入接口
        var lifeTimeInterfaces = interfaces.Where(x => LifeTimeTypes.Contains(x)).Distinct().ToArray();
        if (lifeTimeInterfaces.Length > 1) throw new ApplicationException($"{type.FullName}不允许存在多个生命周期定义");

        //具体的生命周期类型
        var currentLifeTimeInterface = lifeTimeInterfaces[0];

        //能够注册的接口
        var ableRegisterInterfaces = interfaces
            .Where(x => x != currentLifeTimeInterface)
            .Where(x => x != typeof(IDependency));

        return (GetRegisterType(currentLifeTimeInterface), ableRegisterInterfaces.ToArray());
    }


    /// <summary>
    /// 判断生命周期
    /// </summary>
    /// <param name="lifeTimeType"></param>
    /// <returns></returns>
    /// <exception cref="InvalidCastException"></exception>
    public static ServiceLifetime GetRegisterType(MemberInfo lifeTimeType)
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

    public static void Register(IServiceCollection services, ServiceLifetime lifeTime, Type service, Type interfaceType = null)
    {
        //范型类型转换
        if (service.IsGenericType)
        {
            service = service.Assembly.GetType(service.Namespace + "." + service.Name);
            if (service == null)
                throw new NullReferenceException(nameof(service));
        }

        if (interfaceType == null)
            services.Add(ServiceDescriptor.Describe(service, service, lifeTime));
        else
        {
            if (interfaceType.IsGenericType)
            {
                interfaceType = interfaceType.Assembly.GetType(interfaceType.Namespace + "." + interfaceType.Name);
                if (interfaceType == null)
                    throw new NullReferenceException(nameof(interfaceType));
            }

            services.Add(ServiceDescriptor.Describe(interfaceType, service, lifeTime));
        }
    }
}
