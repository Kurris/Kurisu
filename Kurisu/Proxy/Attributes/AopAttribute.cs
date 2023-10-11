using System;

namespace Kurisu.Proxy.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Interface, Inherited = true)]
public class AopAttribute : Attribute
{
    public AopAttribute(params Type[] interceptors)
    {
        Interceptors = interceptors;
    }

    public Type[] Interceptors { get; }
}