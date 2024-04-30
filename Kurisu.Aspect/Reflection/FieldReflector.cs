using System.Reflection;
using System.Reflection.Emit;
using Kurisu.Aspect.Reflection.Emit;

namespace Kurisu.Aspect.Reflection;

internal class FieldReflector : MemberReflector<FieldInfo>
{
    protected Func<object, object> Getter { get; }
    protected Action<object, object> Setter { get; }

    public FieldReflector(FieldInfo reflectionInfo) : base(reflectionInfo)
    {
        Getter = CreateGetter();
        Setter = CreateSetter();
    }

    protected virtual Func<object, object> CreateGetter()
    {
        var dynamicMethod = new DynamicMethod($"getter-{Guid.NewGuid()}", typeof(object), new Type[] { typeof(object) }, Current.Module, true);
        var ilGen = dynamicMethod.GetILGenerator();
        ilGen.EmitLoadArgument(0);
        ilGen.EmitConvertFromObject(Current.DeclaringType);
        ilGen.Emit(OpCodes.Ldfld, Current);
        ilGen.EmitConvertToObject(Current.FieldType);
        ilGen.Emit(OpCodes.Ret);
        return (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>));
    }

    protected virtual Action<object, object> CreateSetter()
    {
        var dynamicMethod = new DynamicMethod($"setter-{Guid.NewGuid()}", typeof(void), new Type[] { typeof(object), typeof(object) }, Current.Module, true);
        var ilGen = dynamicMethod.GetILGenerator();
        ilGen.EmitLoadArgument(0);
        ilGen.EmitConvertFromObject(Current.DeclaringType);
        ilGen.EmitLoadArgument(1);
        ilGen.EmitConvertFromObject(Current.FieldType);
        ilGen.Emit(OpCodes.Stfld, Current);
        ilGen.Emit(OpCodes.Ret);
        return (Action<object, object>)dynamicMethod.CreateDelegate(typeof(Action<object, object>));
    }

    public virtual object GetValue(object instance)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        return Getter(instance);
    }

    public virtual void SetValue(object instance, object value)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        Setter(instance, value);
    }

    public virtual object GetStaticValue()
    {
        throw new InvalidOperationException($"Field {Current.Name} must be static to call this method. For get instance field value, call 'GetValue'.");
    }

    public virtual void SetStaticValue(object value)
    {
        throw new InvalidOperationException($"Field {Current.Name} must be static to call this method. For set instance field value, call 'SetValue'.");
    }
}