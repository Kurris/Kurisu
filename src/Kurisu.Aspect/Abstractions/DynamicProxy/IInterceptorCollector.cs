using System.Reflection;

namespace AspectCore.DynamicProxy;

[NonAspect]
public interface IInterceptorCollector
{
    /// <summary>
    /// 收集拦截器
    /// </summary>
    /// <param name="serviceMethod"></param>
    /// <param name="implementationMethod"></param>
    /// <returns></returns>
    IEnumerable<IInterceptor> Collect(MethodInfo serviceMethod, MethodInfo implementationMethod);
}