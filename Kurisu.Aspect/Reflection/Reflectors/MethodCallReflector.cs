﻿using System.Reflection;
using System.Reflection.Emit;
using Kurisu.Aspect.Reflection.Emit;
using Kurisu.Aspect.Reflection.Extensions;

namespace Kurisu.Aspect.Reflection.Reflectors;

internal class MethodCallReflector : MethodReflector
{
    public MethodCallReflector(MethodInfo reflectionInfo)
        : base(reflectionInfo)
    {
    }

    protected override Func<object, object[], object> CreateInvoker()
    {
        var dynamicMethod = new DynamicMethod($"invoker_{DisplayName}",
            typeof(object), new[] { typeof(object), typeof(object[]) }, Current.Module, true);

        var ilGen = dynamicMethod.GetILGenerator();
        var parameterTypes = Current.GetParameterTypes();

        ilGen.EmitLoadArgument(0);
        ilGen.EmitConvertFromObject(Current.DeclaringType);
        if (Current.DeclaringType!.GetTypeInfo().IsValueType)
        {
            var local = ilGen.DeclareLocal(Current.DeclaringType);
            ilGen.Emit(OpCodes.Stloc, local);
            ilGen.Emit(OpCodes.Ldloca, local);
        }

        if (parameterTypes.Length == 0)
        {
            return CreateDelegate();
        }

        var refParameterCount = parameterTypes.Count(x => x.IsByRef);
        if (refParameterCount == 0)
        {
            for (var i = 0; i < parameterTypes.Length; i++)
            {
                ilGen.EmitLoadArgument(1);
                ilGen.EmitInt(i);
                ilGen.Emit(OpCodes.Ldelem_Ref);
                ilGen.EmitConvertFromObject(parameterTypes[i]);
            }

            return CreateDelegate();
        }

        var indexedLocals = new IndexedLocalBuilder[refParameterCount];
        var index = 0;
        for (var i = 0; i < parameterTypes.Length; i++)
        {
            ilGen.EmitLoadArgument(1);
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

        return CreateDelegate(() =>
        {
            foreach (var t in indexedLocals)
            {
                ilGen.EmitLoadArgument(1);
                ilGen.EmitInt(t.Index);
                ilGen.Emit(OpCodes.Ldloc, t.LocalBuilder);
                ilGen.EmitConvertToObject(t.LocalType);
                ilGen.Emit(OpCodes.Stelem_Ref);
            }
        });

        Func<object, object[], object> CreateDelegate(Action callback = null)
        {
            ilGen.EmitCall(OpCodes.Call, Current, null);
            callback?.Invoke();
            if (Current.ReturnType == typeof(void)) ilGen.Emit(OpCodes.Ldnull);
            else if (Current.ReturnType.GetTypeInfo().IsValueType)
                ilGen.EmitConvertToObject(Current.ReturnType);
            ilGen.Emit(OpCodes.Ret);
            return (Func<object, object[], object>)dynamicMethod.CreateDelegate(typeof(Func<object, object[], object>));
        }
    }
}