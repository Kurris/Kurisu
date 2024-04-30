using System.Reflection;
using Kurisu.Aspect.Reflection.Factories;

namespace Kurisu.Aspect.Reflection.Extensions;

internal static class ReflectorExtensions
{
    #region Reflection

    public static TypeReflector GetReflector(this Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return TypeReflectorFactory.Create(type.GetTypeInfo());
    }

    public static TypeReflector GetReflector(this TypeInfo typeInfo)
    {
        if (typeInfo == null)
        {
            throw new ArgumentNullException(nameof(typeInfo));
        }

        return TypeReflectorFactory.Create(typeInfo);
    }

    public static ConstructorReflector GetReflector(this ConstructorInfo constructor)
    {
        if (constructor == null)
        {
            throw new ArgumentNullException(nameof(constructor));
        }

        return ConstructorReflectorFactory.Create(constructor);
    }

    public static FieldReflector GetReflector(this FieldInfo field)
    {
        if (field == null)
        {
            throw new ArgumentNullException(nameof(field));
        }

        return FieldReflectorFactory.Create(field);
    }

    public static MethodReflector GetReflector(this MethodInfo method)
    {
        return GetReflector(method, CallOptions.Callvirt);
    }

    public static MethodReflector GetReflector(this MethodInfo method, CallOptions callOption)
    {
        if (method == null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        return MethodReflectorFactory.Create(method, callOption);
    }

    public static PropertyReflector GetReflector(this PropertyInfo property)
    {
        if (property == null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        return GetReflector(property, CallOptions.Callvirt);
    }

    public static PropertyReflector GetReflector(this PropertyInfo property, CallOptions callOption)
    {
        if (property == null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        return PropertyReflectorFactory.Create(property, callOption);
    }

    public static ParameterReflector GetReflector(this ParameterInfo parameterInfo)
    {
        if (parameterInfo == null)
        {
            throw new ArgumentNullException(nameof(parameterInfo));
        }

        return ParameterReflectorFactory.Create(parameterInfo);
    }

    #endregion

    #region Reflectr

    // public static FieldInfo GetFieldInfo(this FieldReflector reflector) => reflector?.GetMemberInfo();
    //
    // public static MethodInfo GetMethodInfo(this MethodReflector reflector) => reflector?.GetMemberInfo();
    //
    // public static ConstructorInfo GetConstructorInfo(this ConstructorReflector reflector) => reflector?.GetMemberInfo();
    //
    // public static PropertyInfo GetPropertyInfo(this PropertyReflector reflector) => reflector?.GetMemberInfo();

    #endregion
}