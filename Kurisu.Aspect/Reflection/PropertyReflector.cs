using System.Reflection;
using System.Reflection.Emit;
using Kurisu.Aspect.Reflection.Emit;

namespace Kurisu.Aspect.Reflection;

internal partial class PropertyReflector : MemberReflector<PropertyInfo>
{
    protected Func<object, object> Getter { get; }
    protected Action<object, object> Setter { get; }

    public PropertyReflector(PropertyInfo reflectionInfo) : base(reflectionInfo)
    {
        Getter = reflectionInfo.CanRead ? CreateGetter() : ins => throw new InvalidOperationException($"Property {Current.Name} does not define get accessor.");
        Setter = reflectionInfo.CanWrite ? CreateSetter() : (ins, val) => { throw new InvalidOperationException($"Property {Current.Name} does not define get accessor."); };
    }

    protected virtual Func<object, object> CreateGetter()
    {
        var dynamicMethod = new DynamicMethod($"getter-{Guid.NewGuid()}", typeof(object), new Type[] { typeof(object) }, Current.Module, true);
        var ilGen = dynamicMethod.GetILGenerator();
        ilGen.EmitLoadArgument(0);
        ilGen.EmitConvertFromObject(Current.DeclaringType);
        ilGen.Emit(OpCodes.Callvirt, Current.GetMethod!);
        if (Current.PropertyType.GetTypeInfo().IsValueType)
            ilGen.EmitConvertToObject(Current.PropertyType);
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
        ilGen.EmitConvertFromObject(Current.PropertyType);
        ilGen.Emit(OpCodes.Callvirt, Current.SetMethod!);
        ilGen.Emit(OpCodes.Ret);
        return (Action<object, object>)dynamicMethod.CreateDelegate(typeof(Action<object, object>));
    }

    public virtual object GetValue(object instance)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        return Getter.Invoke(instance);
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
        throw new InvalidOperationException($"Property {Current.Name} must be static to call this method. For get instance property value, call 'GetValue'.");
    }

    public virtual void SetStaticValue(object value)
    {
        throw new InvalidOperationException($"Property {Current.Name} must be static to call this method. For set instance property value, call 'SetValue'.");
    }
}