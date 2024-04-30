using System.Reflection;
using System.Reflection.Emit;
using Kurisu.Aspect.Reflection.Emit;

namespace Kurisu.Aspect.Reflection;

internal class FieldEnumReflector : FieldReflector
{
    public FieldEnumReflector(FieldInfo reflectionInfo) : base(reflectionInfo)
    {
    }

    protected override Func<object, object> CreateGetter()
    {
        var dynamicMethod = new DynamicMethod($"getter-{Guid.NewGuid()}", typeof(object), new[] { typeof(object) }, Current.Module, true);
        var ilGen = dynamicMethod.GetILGenerator();
        var value = Current.GetValue(null);
        ilGen.EmitConstant(value, Current.FieldType);
        ilGen.EmitConvertToObject(Current.FieldType);
        ilGen.Emit(OpCodes.Ret);
        return (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>));
    }

    protected override Action<object, object> CreateSetter()
    {
        var dynamicMethod = new DynamicMethod($"setter-{Guid.NewGuid()}", typeof(void), new[] { typeof(object), typeof(object) }, Current.Module, true);
        var ilGen = dynamicMethod.GetILGenerator();
        ilGen.Emit(OpCodes.Ret);
        return (Action<object, object>)dynamicMethod.CreateDelegate(typeof(Action<object, object>));
    }

    public override object GetValue(object instance)
    {
        return Getter(null);
    }

    public override void SetValue(object instance, object value)
    {
        throw new FieldAccessException("Cannot set a constant field");
    }

    public override object GetStaticValue()
    {
        return Getter(null);
    }

    public override void SetStaticValue(object value)
    {
        throw new FieldAccessException("Cannot set a constant field");
    }
}