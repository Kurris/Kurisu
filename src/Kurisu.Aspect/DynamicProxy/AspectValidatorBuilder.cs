using AspectCore.Configuration;

namespace AspectCore.DynamicProxy;

[NonAspect]
public sealed class AspectValidatorBuilder : IAspectValidatorBuilder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IList<Func<AspectValidationDelegate, AspectValidationDelegate>> _collections;

    public AspectValidatorBuilder(AspectConfiguration aspectConfiguration, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _collections = new List<Func<AspectValidationDelegate, AspectValidationDelegate>>();

        foreach (var handler in aspectConfiguration.ValidationHandlers.OrderBy(x => x.Order))
        {
            _collections.Add(next => method => handler.Invoke(method, next));
        }
    }

    public IAspectValidator Build()
    {
        AspectValidationDelegate invoke = method => false;

        var count = _collections.Count;

        for (var i = count - 1; i > -1; i--)
        {
            invoke = _collections[i](invoke);
        }

        return new AspectValidator(_serviceProvider, invoke);
    }
}