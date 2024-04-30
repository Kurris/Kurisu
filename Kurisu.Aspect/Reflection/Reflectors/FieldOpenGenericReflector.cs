using System.Reflection;

namespace Kurisu.Aspect.Reflection.Reflectors;

internal class FieldOpenGenericReflector : FieldReflector
{
    public FieldOpenGenericReflector(FieldInfo reflectionInfo) : base(reflectionInfo)
    {
    }

    protected override Func<object, object> CreateGetter() => null;

    protected override Action<object, object> CreateSetter() => null;

    public override object GetValue(object instance) => throw new InvalidOperationException("Late bound operations cannot be performed on fields with types for which Type.ContainsGenericParameters is true");

    public override void SetValue(object instance, object value) => throw new InvalidOperationException("Late bound operations cannot be performed on fields with types for which Type.ContainsGenericParameters is true");

    public override object GetStaticValue() => throw new InvalidOperationException("Late bound operations cannot be performed on fields with types for which Type.ContainsGenericParameters is true");

    public override void SetStaticValue(object value) => throw new InvalidOperationException("Late bound operations cannot be performed on fields with types for which Type.ContainsGenericParameters is true");
}