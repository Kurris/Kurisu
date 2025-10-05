using System.Reflection;

namespace AspectCore.DynamicProxy.Parameters;

public class Parameter
{
    protected readonly AspectContext Context;
    protected readonly int Index;

    public string Name { get; }

    public Type Type { get; }

    public Type RawType
    {
        get
        {
            if (IsRef)
            {
                return Type.GetElementType();
            }

            return Type;
        }
    }

    public bool IsRef { get; }

    public virtual object Value
    {
        get => Context.Parameters[Index];
        set => Context.Parameters[Index] = value;
    }

    public ParameterInfo ParameterInfo { get; }

    internal Parameter(AspectContext context, int index, ParameterInfo parameterInfo)
    {
        Context = context;
        Index = index;
        Name = parameterInfo.Name;
        Type = parameterInfo.ParameterType;
        IsRef = Type.IsByRef;
        ParameterInfo = parameterInfo;
    }
}