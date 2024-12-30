using System.Reflection;

namespace Kurisu.Aspect.Reflection.Reflectors;

internal sealed class TypeReflector : MemberReflector<TypeInfo>
{
    public TypeReflector(TypeInfo typeInfo) : base(typeInfo)
    {
        DisplayName = GetDisplayName(typeInfo);
        FullDisplayName = GetFullDisplayName(typeInfo);
    }

    public override string DisplayName { get; }

    public string FullDisplayName { get; }

    private static string GetDisplayName(TypeInfo typeInfo)
    {
        var name = typeInfo.Name.Replace('+', '.');
        if (typeInfo.IsGenericParameter)
        {
            return name;
        }

        // ReSharper disable once InvertIf
        if (typeInfo.IsGenericType)
        {
            var arguments = typeInfo.IsGenericTypeDefinition
                ? typeInfo.GenericTypeParameters
                : typeInfo.GenericTypeArguments;
            name = name.Replace("`", "").Replace(arguments.Length.ToString(), "");
            name += $"<{GetDisplayName(arguments[0].GetTypeInfo())}";
            for (var i = 1; i < arguments.Length; i++)
            {
                name = name + "," + GetDisplayName(arguments[i].GetTypeInfo());
            }

            name += ">";
        }

        return !typeInfo.IsNested ? name : $"{GetDisplayName(typeInfo.DeclaringType!.GetTypeInfo())}.{name}";
    }

    private static string GetFullDisplayName(TypeInfo typeInfo)
    {
        var name = typeInfo.Name.Replace('+', '.');
        if (typeInfo.IsGenericParameter)
        {
            return name;
        }

        if (!typeInfo.IsNested)
        {
            name = $"{typeInfo.Namespace}." + name;
        }
        else
        {
            name = $"{GetFullDisplayName(typeInfo.DeclaringType!.GetTypeInfo())}.{name}";
        }

        // ReSharper disable once InvertIf
        if (typeInfo.IsGenericType)
        {
            var arguments = typeInfo.IsGenericTypeDefinition
                ? typeInfo.GenericTypeParameters
                : typeInfo.GenericTypeArguments;
            name = name.Replace("`", "").Replace(arguments.Length.ToString(), "");
            name += $"<{GetFullDisplayName(arguments[0].GetTypeInfo())}";
            for (var i = 1; i < arguments.Length; i++)
            {
                name += "," + GetFullDisplayName(arguments[i].GetTypeInfo());
            }

            name += ">";
        }

        return name;
    }
}