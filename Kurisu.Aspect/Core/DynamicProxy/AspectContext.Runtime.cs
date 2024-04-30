using System.Reflection;
using Kurisu.Aspect.Core.DynamicProxy.Extensions;
using Kurisu.Aspect.Core.Utils;
using Kurisu.Aspect.DynamicProxy;
using Kurisu.Aspect.Reflection;
using Kurisu.Aspect.Reflection.Extensions;

namespace Kurisu.Aspect.Core.DynamicProxy;

[NonAspect]
internal sealed class RuntimeAspectContext : AspectContext
{
    public override IServiceProvider ServiceProvider { get; }

    public override object ReturnValue { get; set; }

    public override MethodInfo ServiceMethod { get; }

    public override object[] Parameters { get; }

    public override MethodInfo ProxyMethod { get; }

    public override object Proxy { get; }

    public override MethodInfo ImplementationMethod { get; }

    public override object Implementation { get; }

    public RuntimeAspectContext(IServiceProvider serviceProvider, MethodInfo serviceMethod, MethodInfo targetMethod, MethodInfo proxyMethod,
        object targetInstance, object proxyInstance, object[] parameters)
    {
        ServiceProvider = serviceProvider;
        ImplementationMethod = targetMethod;
        Implementation = targetInstance;
        ServiceMethod = serviceMethod;
        ProxyMethod = proxyMethod;
        Proxy = proxyInstance;
        Parameters = parameters;
    }

    public override async Task Complete()
    {
        if (Implementation == null || ImplementationMethod == null)
        {
            await Break();
            return;
        }

        var reflector = AspectContextRuntimeExtensions.ReflectorTable.GetOrAdd(ImplementationMethod, m => m.GetReflector(m.IsCallvirt() ? CallOptions.Callvirt : CallOptions.Call));
        var returnValue = reflector.Invoke(Implementation, Parameters);
        await this.AwaitIfAsync();
        ReturnValue = returnValue;
    }

    public override Task Break()
    {
        ReturnValue ??= ServiceMethod.ReturnParameter.ParameterType.GetDefaultValue();

        return Task.CompletedTask;
    }

    public override Task Invoke(AspectDelegate next)
    {
        return next(this);
    }
}