using System.Reflection;
using System.Reflection.Emit;
using Kurisu.Aspect.Reflection.Emit;

namespace Kurisu.Aspect.Reflection;

internal class FieldStaticReflector : FieldReflector
{
    public FieldStaticReflector(FieldInfo reflectionInfo) : base(reflectionInfo)
    {
    }

    protected override Func<object, object> CreateGetter()
    {
        var dynamicMethod = new DynamicMethod($"getter-{Guid.NewGuid()}", typeof(object), new Type[] { typeof(object) }, Current.Module, true);
        var ilGen = dynamicMethod.GetILGenerator();
        ilGen.Emit(OpCodes.Ldsfld, Current);
        ilGen.EmitConvertToObject(Current.FieldType);
        ilGen.Emit(OpCodes.Ret);
        return (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>));
    }

    protected override Action<object, object> CreateSetter()
    {
        var dynamicMethod = new DynamicMethod($"setter-{Guid.NewGuid()}", typeof(void), new Type[] { typeof(object), typeof(object) }, Current.Module, true);
        var ilGen = dynamicMethod.GetILGenerator();
        ilGen.EmitLoadArgument(1);
        ilGen.EmitConvertFromObject(Current.FieldType);
        ilGen.Emit(OpCodes.Stsfld, Current);
        ilGen.Emit(OpCodes.Ret);
        return (Action<object, object>)dynamicMethod.CreateDelegate(typeof(Action<object, object>));
    }

    public override object GetValue(object instance)
    {
        return Getter(null);
    }

    public override void SetValue(object instance, object value)
    {
        Setter(null, value);
    }

    public override object GetStaticValue()
    {
        return Getter(null);
    }

    public override void SetStaticValue(object value)
    {
        Setter(null, value);
    }
}