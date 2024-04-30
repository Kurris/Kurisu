using System.Reflection;

namespace Kurisu.Aspect.Reflection;

internal class ConstructorOpenGenericReflector : ConstructorReflector
{
    public ConstructorOpenGenericReflector(ConstructorInfo constructorInfo) : base(constructorInfo)
    {
    }

    protected override Func<object[], object> CreateInvoker() => null;

    public override object Invoke(params object[] args) => throw new InvalidOperationException($"Cannot create an instance of {Current.DeclaringType} because Type.ContainsGenericParameters is true.");
}