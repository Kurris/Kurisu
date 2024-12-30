using System.Reflection;
using System.Reflection.Emit;
using Kurisu.Aspect.Reflection.Emit;
using Kurisu.Aspect.Reflection.Extensions;
using Kurisu.Aspect.Reflection.Factories;

namespace Kurisu.Aspect.Reflection.Reflectors;

internal class ConstructorReflector : MemberReflector<ConstructorInfo>, IParameterReflectorProvider
{
    private readonly Func<object[], object> _invoker;

    public ParameterReflector[] ParameterReflectors { get; }

    public ConstructorReflector(ConstructorInfo constructorInfo) : base(constructorInfo)
    {
        _invoker = CreateInvoker();
        ParameterReflectors = constructorInfo.GetParameters().Select(ParameterReflectorFactory.Create).ToArray();
    }

    protected virtual Func<object[], object> CreateInvoker()
    {
        var dynamicMethod = new DynamicMethod($"invoker-{Guid.NewGuid()}", typeof(object), new[] { typeof(object[]) }, Current.Module, true);
        var ilGen = dynamicMethod.GetILGenerator();

        var parameterTypes = Current.GetParameterTypes();
        if (parameterTypes.Length == 0)
        {
            ilGen.Emit(OpCodes.Newobj, Current);
            return CreateDelegate();
        }

        var refParameterCount = parameterTypes.Count(x => x.IsByRef);
        if (refParameterCount == 0)
        {
            for (var i = 0; i < parameterTypes.Length; i++)
            {
                ilGen.EmitLoadArgument(0);
                ilGen.EmitInt(i);
                ilGen.Emit(OpCodes.Ldelem_Ref);
                ilGen.EmitConvertFromObject(parameterTypes[i]);
            }

            ilGen.Emit(OpCodes.Newobj, Current);
            return CreateDelegate();
        }

        var indexedLocals = new IndexedLocalBuilder[refParameterCount];
        var index = 0;
        for (var i = 0; i < parameterTypes.Length; i++)
        {
            ilGen.EmitLoadArgument(0);
            ilGen.EmitInt(i);
            ilGen.Emit(OpCodes.Ldelem_Ref);
            if (parameterTypes[i].IsByRef)
            {
                var defType = parameterTypes[i].GetElementType()!;
                var indexedLocal = new IndexedLocalBuilder(ilGen.DeclareLocal(defType), i);
                indexedLocals[index++] = indexedLocal;
                ilGen.EmitConvertFromObject(defType);
                ilGen.Emit(OpCodes.Stloc, indexedLocal.LocalBuilder);
                ilGen.Emit(OpCodes.Ldloca, indexedLocal.LocalBuilder);
            }
            else
            {
                ilGen.EmitConvertFromObject(parameterTypes[i]);
            }
        }

        ilGen.Emit(OpCodes.Newobj, Current);
        foreach (var t in indexedLocals)
        {
            ilGen.EmitLoadArgument(0);
            ilGen.EmitInt(t.Index);
            ilGen.Emit(OpCodes.Ldloc, t.LocalBuilder);
            ilGen.EmitConvertToObject(t.LocalType);
            ilGen.Emit(OpCodes.Stelem_Ref);
        }

        return CreateDelegate();

        Func<object[], object> CreateDelegate()
        {
            if (Current.DeclaringType!.GetTypeInfo().IsValueType)
                ilGen.EmitConvertToObject(Current.DeclaringType);
            ilGen.Emit(OpCodes.Ret);
            return (Func<object[], object>)dynamicMethod.CreateDelegate(typeof(Func<object[], object>));
        }
    }

    public virtual object Invoke(params object[] args)
    {
        if (args == null)
        {
            throw new ArgumentNullException(nameof(args));
        }

        return _invoker(args);
    }
}