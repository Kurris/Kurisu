using AspectCore.DynamicProxy;
using AspectCore.DynamicProxy.Parameters;

namespace AspectCore.Configuration;

[NonAspect]
public sealed class AspectConfiguration
{
    public AspectValidationHandlerCollection ValidationHandlers { get; }

    public InterceptorCollection Interceptors { get; }

    public Type PropertyEnableInjectAttributeType { get; set; }

    public bool ThrowAspectException { get; set; }

    public AspectConfiguration()
    {
        ThrowAspectException = false;

        ValidationHandlers = new AspectValidationHandlerCollection()
            .Add(new OverwriteAspectValidationHandler())
            .Add(new AttributeAspectValidationHandler())
            .Add(new CacheAspectValidationHandler());

        Interceptors = new InterceptorCollection()
            .Add(new TypeInterceptorFactory(typeof(EnableParameterAspectInterceptor), Array.Empty<object>()));
    }
}