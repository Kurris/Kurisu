using System.Reflection;
using System.Reflection.Emit;
using Kurisu.Aspect.Reflection.Emit;
using Kurisu.Aspect.Reflection.Extensions;
using Kurisu.Aspect.Reflection.Factories;

namespace Kurisu.Aspect.Reflection.Reflectors;

internal class MethodReflector : MemberReflector<MethodInfo>, IParameterReflectorProvider
{
#if NET8_0
    private readonly MethodInfo _reflectionInfo;
#endif


    public ParameterReflector[] ParameterReflectors { get; }

    public MethodReflector(MethodInfo reflectionInfo) : base(reflectionInfo)
    {
#if NET8_0
     _reflectionInfo = reflectionInfo;
#endif

        DisplayName = GetDisplayName(reflectionInfo);
        // ReSharper disable once VirtualMemberCallInConstructor
        Invoker = CreateInvoker(); //具体实现调用
        ParameterReflectors = reflectionInfo.GetParameters().Select(ParameterReflectorFactory.Create).ToArray();
    }

    public override string DisplayName { get; }
    protected Func<object, object[], object> Invoker { get; }

    /// <summary>
    /// 创建方法调用委托
    /// </summary>
    /// <returns></returns>
    protected virtual Func<object, object[], object> CreateInvoker()
    {
        var dynamicMethod = new DynamicMethod($"invoker_{DisplayName}",
            typeof(object), new[] { typeof(object), typeof(object[]) }, Current.Module, true);

        var ilGen = dynamicMethod.GetILGenerator();
        var parameterTypes = Current.GetParameterTypes();

        ilGen.EmitLoadArgument(0);
        ilGen.EmitConvertFromObject(Current.DeclaringType);

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
#if NET6_0
            //https://github.com/dotnet/runtime/issues/67084
            ilGen.Emit(OpCodes.Callvirt, Current);
#else
            ilGen.EmitCall(OpCodes.Callvirt, _reflectionInfo, null);
#endif
            callback?.Invoke();

            if (Current.ReturnType == typeof(void)) ilGen.Emit(OpCodes.Ldnull);
            else if (Current.ReturnType.GetTypeInfo().IsValueType)
                ilGen.EmitConvertToObject(Current.ReturnType);
            ilGen.Emit(OpCodes.Ret);
            return (Func<object, object[], object>)dynamicMethod.CreateDelegate(typeof(Func<object, object[], object>));
        }
    }

    public virtual object Invoke(object instance, params object[] parameters)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        return Invoker(instance, parameters);
    }

    public virtual object StaticInvoke(params object[] parameters)
    {
        throw new InvalidOperationException($"Method {Current.Name} must be static to call this method. For invoke instance method, call 'Invoke'.");
    }

    private static string GetDisplayName(MethodInfo method)
    {
        var name = $"{method.ReturnType.GetReflector().DisplayName} {method.Name}";
        if (method.IsGenericMethod)
        {
            name += "<";
            var arguments = method.GetGenericArguments();
            name += arguments[0].GetReflector().DisplayName;
            for (var i = 1; i < arguments.Length; i++)
            {
                name += ("," + arguments[i].GetReflector().DisplayName);
            }

            name += ">";
        }

        var parameterTypes = method.GetParameterTypes();
        name += "(";
        if (parameterTypes.Length == 0)
        {
            name += ")";
            return name;
        }

        name += parameterTypes[0].GetReflector().DisplayName;
        for (var i = 1; i < parameterTypes.Length; i++)
        {
            name += "," + parameterTypes[i].GetReflector().DisplayName;
        }

        name += ")";
        return name;
    }
}