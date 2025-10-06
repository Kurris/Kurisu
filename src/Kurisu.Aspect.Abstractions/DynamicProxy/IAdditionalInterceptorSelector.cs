using System.Reflection;

namespace AspectCore.DynamicProxy;

[NonAspect]
public interface IAdditionalInterceptorSelector
{
    IEnumerable<IInterceptor> Select(MethodInfo serviceMethod, MethodInfo implementationMethod);
}