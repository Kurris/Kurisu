namespace AspectCore.DynamicProxy.Parameters;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public abstract class ParameterInterceptorAttribute : Attribute, IParameterInterceptor
{
    public abstract Task Invoke(ParameterAspectContext context, ParameterAspectDelegate next);
}