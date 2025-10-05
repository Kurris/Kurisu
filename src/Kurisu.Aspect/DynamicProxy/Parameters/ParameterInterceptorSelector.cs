using System.Reflection;
using AspectCore.Extensions.Reflection;

namespace AspectCore.DynamicProxy.Parameters;

public sealed class ParameterInterceptorSelector : IParameterInterceptorSelector
{
    private readonly AspectCaching<ParameterInterceptorSelector, IParameterInterceptor[]> _aspectCaching;

    public ParameterInterceptorSelector(AspectCaching<ParameterInterceptorSelector, IParameterInterceptor[]> aspectCaching)
    {
        _aspectCaching = aspectCaching;
    }

    public IParameterInterceptor[] Select(ParameterInfo parameter)
    {
        if (parameter == null)
        {
            throw new ArgumentNullException(nameof(parameter));
        }

        return _aspectCaching.GetOrAdd(parameter, info =>
        {
            var interceptors = ((ParameterInfo)info).GetReflector().GetCustomAttributes().OfType<IParameterInterceptor>().ToArray();
            return interceptors;
        });
    }
}