using System.Reflection;
using System.Reflection.Emit;
using Kurisu.Aspect.Reflection.Emit;
using Kurisu.Aspect.Reflection.Extensions;
using Kurisu.Aspect.Reflection.Internals;

namespace Kurisu.Aspect.Reflection.Reflectors;

internal partial class CustomAttributeReflector
{
    private readonly CustomAttributeData _customAttributeData;
    private readonly Func<Attribute> _invoker;

    internal  HashSet<RuntimeTypeHandle> Tokens { get; }

    public Type AttributeType { get; }

    public CustomAttributeReflector(CustomAttributeData customAttributeData)
    {
        _customAttributeData = customAttributeData ?? throw new ArgumentNullException(nameof(customAttributeData));
        AttributeType = _customAttributeData.AttributeType;
        _invoker = CreateInvoker();
        Tokens = GetAttrTokens(AttributeType);
    }

    private Func<Attribute> CreateInvoker()
    {
        var dynamicMethod = new DynamicMethod($"invoker-{Guid.NewGuid()}", typeof(Attribute), null, AttributeType.GetTypeInfo().Module, true);
        var ilGen = dynamicMethod.GetILGenerator();

        foreach (var constructorParameter in _customAttributeData.ConstructorArguments)
        {
            if (constructorParameter.ArgumentType.IsArray)
            {
                ilGen.EmitArray(((IEnumerable<CustomAttributeTypedArgument>)constructorParameter.Value!).Select(x => x.Value).ToArray(),
                    constructorParameter.ArgumentType.GetTypeInfo().UnWrapArrayType());
            }
            else
            {
                ilGen.EmitConstant(constructorParameter.Value, constructorParameter.ArgumentType);
            }
        }

        var attributeLocal = ilGen.DeclareLocal(AttributeType);
        ilGen.EmitNew(_customAttributeData.Constructor);
        ilGen.Emit(OpCodes.Stloc, attributeLocal);

        var attributeTypeInfo = AttributeType.GetTypeInfo();

        foreach (var namedArgument in _customAttributeData.NamedArguments!)
        {
            ilGen.Emit(OpCodes.Ldloc, attributeLocal);
            if (namedArgument.TypedValue.ArgumentType.IsArray)
            {
                ilGen.EmitArray(((IEnumerable<CustomAttributeTypedArgument>)namedArgument.TypedValue.Value!).Select(x => x.Value).ToArray(),
                    namedArgument.TypedValue.ArgumentType.GetTypeInfo().UnWrapArrayType());
            }
            else
            {
                ilGen.EmitConstant(namedArgument.TypedValue.Value, namedArgument.TypedValue.ArgumentType);
            }

            if (namedArgument.IsField)
            {
                var field = attributeTypeInfo.GetField(namedArgument.MemberName);
                ilGen.Emit(OpCodes.Stfld, field!);
            }
            else
            {
                var property = attributeTypeInfo.GetProperty(namedArgument.MemberName);
                ilGen.Emit(OpCodes.Callvirt, property!.SetMethod!);
            }
        }

        ilGen.Emit(OpCodes.Ldloc, attributeLocal);
        ilGen.Emit(OpCodes.Ret);
        return (Func<Attribute>)dynamicMethod.CreateDelegate(typeof(Func<Attribute>));
    }

    private HashSet<RuntimeTypeHandle> GetAttrTokens(Type attributeType)
    {
        var tokenSet = new HashSet<RuntimeTypeHandle>();
        for (var attr = attributeType; attr != typeof(object); attr = attr.GetTypeInfo().BaseType)
        {
            tokenSet.Add(attr!.TypeHandle);
        }
    
        return tokenSet;
    }

    public Attribute Invoke()
    {
        return _invoker();
    }

    public CustomAttributeData GetCustomAttributeData()
    {
        return _customAttributeData;
    }
}

internal partial class CustomAttributeReflector
{
    public static CustomAttributeReflector Create(CustomAttributeData customAttributeData)
    {
        return ReflectorCacheUtils<CustomAttributeData, CustomAttributeReflector>.GetOrAdd(customAttributeData, data => new CustomAttributeReflector(data));
    }
}