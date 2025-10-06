using System.Reflection;

namespace AspectCore.DynamicProxy.Parameters;

internal sealed class ReturnParameter : Parameter
{
    public override object Value
    {
        get => Context.ReturnValue;
        set => Context.ReturnValue = value;
    }

    internal ReturnParameter(AspectContext context, ParameterInfo reflector)
        : base(context, -1, reflector)
    {
    }
}