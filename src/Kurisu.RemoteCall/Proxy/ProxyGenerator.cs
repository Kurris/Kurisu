using System.Reflection;
using Kurisu.RemoteCall.Attributes;
using Kurisu.RemoteCall.Default;
using Kurisu.RemoteCall.Proxy.Abstractions;

namespace Kurisu.RemoteCall.Proxy;

/// <summary>
/// 代理生成器
/// </summary>
internal class ProxyGenerator : DispatchProxy
{
    private static readonly MethodInfo CreateMethod = typeof(DispatchProxy)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .FirstOrDefault(x => x.IsGenericMethod && x.Name == nameof(DispatchProxy.Create));

    /// <summary>
    /// DispatchProxy需要无参构造函数
    /// </summary>
    // ReSharper disable once EmptyConstructor
    public ProxyGenerator()
    {
    }

    /// <summary>
    /// 代理实现
    /// </summary>
    private IInterceptor Interceptor { get; set; }

    /// <summary>
    /// 代理接口
    /// </summary>
    private Type InterfaceType { get; set; }

    /// <summary>
    /// 服务提供器
    /// </summary>
    private IServiceProvider ServiceProvider { get; set; }

    /// <summary>
    /// 创建代理对象
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="interfaceType"></param>
    /// <param name="interceptor"></param>
    /// <returns></returns>
    public static object Create(IServiceProvider serviceProvider, Type interfaceType, IInterceptor interceptor)
    {
        var proxy = (ProxyGenerator)CreateMethod!.MakeGenericMethod(interfaceType, typeof(ProxyGenerator)).Invoke(null, null)!;
        proxy.Interceptor = interceptor;
        proxy.InterfaceType = interfaceType;
        proxy.ServiceProvider = serviceProvider;
        return proxy;
    }

    /// <summary>
    /// 代理方法执行
    /// </summary>
    /// <param name="method"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    protected override object Invoke(MethodInfo method, object[] args)
    {
        var parameterInfos = method!.GetParameters();
        var parameterValues = parameterInfos.Select((p, i) => new ParameterValue(p, args![i])).ToList();
        var enableRemoteClientAttribute = InterfaceType.GetCustomAttribute<EnableRemoteClientAttribute>()!;

        var info = new ProxyInfo
        {
            ServiceProvider = ServiceProvider,

            Method = method,
            ParameterInfos = parameterInfos,
            ParameterValues = args,
            WrapParameterValues = parameterValues,
            InterfaceType = InterfaceType,
            RemoteClient = new RemoteClient
            {
                Name = enableRemoteClientAttribute.Name,
                BaseUrl = enableRemoteClientAttribute.BaseUrl,
                PolicyHandler = enableRemoteClientAttribute.PolicyHandler
            }
        };

        Interceptor.Intercept(info);
        return info.ReturnValue;
    }
}