using System.Reflection;
using AspectCore.DynamicProxy.Parameters;
using Microsoft.Extensions.DependencyInjection;

namespace AspectCore.DynamicProxy;

[NonAspect]
public sealed class AttributeAspectValidationHandler : IAspectValidationHandler
{
    public int Order { get; } = 13;

    public bool Invoke(AspectValidationContext context, AspectValidationDelegate next)
    {
        var declaringType = context.Method.DeclaringType!.GetTypeInfo();
        var parameterInfos = context.Method.GetParameters();
        var parameterInterceptorSelector = context.ServiceProvider.GetRequiredService<IParameterInterceptorSelector>();
        
        if (IsAttributeAspect(declaringType) || IsAttributeAspect(context.Method) || parameterInfos.Any(p => parameterInterceptorSelector.Select(p).Length > 0))
        {
            return true;
        }

        return next(context);
    }

    private bool IsAttributeAspect(MemberInfo member)
    {
        return member.CustomAttributes
            .Any(data => typeof(IInterceptor).GetTypeInfo()
                .IsAssignableFrom(data.AttributeType));
    }
}