using System.Reflection;
using Kurisu.Aspect.DynamicProxy;

namespace Kurisu.Aspect.Reflection.Extensions;

internal static class ReflectionUtils
{
    internal static bool IsProxy(this object instance)
    {
        return instance != null && instance.GetType().GetTypeInfo().IsProxyType();
    }

    private static bool IsProxyType(this TypeInfo typeInfo)
    {
        if (typeInfo == null)
        {
            throw new ArgumentNullException(nameof(typeInfo));
        }

        return typeInfo.GetReflector().IsDefined(typeof(DynamicallyAttribute));
    }

    internal static bool CanInherited(this TypeInfo typeInfo)
    {
        if (typeInfo == null) throw new ArgumentNullException(nameof(typeInfo));

        if (typeInfo.IsValueType || typeInfo.IsEnum || typeInfo.IsSealed || typeInfo.IsProxyType())
        {
            return false;
        }

        return typeInfo.IsVisible();
    }

    internal static Type[] GetParameterTypes(this MethodBase method)
    {
        if (method == null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        return method.GetParameters().Select(p => p.ParameterType).ToArray();
    }

    private static bool IsNonAspect(this TypeInfo typeInfo)
    {
        if (typeInfo == null)
        {
            throw new ArgumentNullException(nameof(typeInfo));
        }

        return typeInfo.GetReflector().IsDefined(typeof(NonAspectAttribute));
    }

    internal static bool IsNonAspect(this MethodInfo methodInfo)
    {
        if (methodInfo == null)
        {
            throw new ArgumentNullException(nameof(methodInfo));
        }

        return methodInfo.DeclaringType!.GetTypeInfo().IsNonAspect() || methodInfo.GetReflector().IsDefined(typeof(NonAspectAttribute));
    }

    /// <summary>
    /// 是否为可重载方法
    /// </summary>
    /// <param name="methodInfo"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    internal static bool IsCallvirt(this MethodInfo methodInfo)
    {
        if (methodInfo == null) throw new ArgumentNullException(nameof(methodInfo));

        if (methodInfo.IsExplicit()) return true;

        var typeInfo = methodInfo.DeclaringType!.GetTypeInfo();

        return !typeInfo.IsClass;
    }

    internal static bool IsExplicit(this MethodInfo methodInfo)
    {
        if (methodInfo == null)
        {
            throw new ArgumentNullException(nameof(methodInfo));
        }

        return methodInfo.Attributes.HasFlag(MethodAttributes.Private | MethodAttributes.Final |
                                             MethodAttributes.Virtual);
    }

    internal static bool IsVoid(this MethodInfo methodInfo)
    {
        return methodInfo.ReturnType == typeof(void);
    }

    internal static string GetDisplayName(this PropertyInfo member)
    {
        if (member == null) throw new ArgumentNullException(nameof(member));

        var declaringType = member.DeclaringType!.GetTypeInfo();

        return declaringType.IsInterface
            ? $"{declaringType.Namespace}.{declaringType.GetReflector().DisplayName}.{member.Name}"
            : member.Name;
    }

    internal static string GetName(this MethodInfo member)
    {
        if (member == null) throw new ArgumentNullException(nameof(member));

        var declaringType = member.DeclaringType!.GetTypeInfo();

        return declaringType.IsInterface
            ? $"{declaringType.Namespace}.{declaringType.GetReflector().DisplayName}.{member.Name}"
            : member.Name;
    }

    internal static string GetDisplayName(this MethodInfo member)
    {
        if (member == null) throw new ArgumentNullException(nameof(member));

        var declaringType = member.DeclaringType!.GetTypeInfo();
        return $"{declaringType.Namespace}.{declaringType.GetReflector().DisplayName}.{member.GetReflector().DisplayName}";
    }

    // public static bool IsReturnTask(this MethodInfo methodInfo)
    // {
    //     if (methodInfo == null)
    //     {
    //         throw new ArgumentNullException(nameof(methodInfo));
    //     }
    //     var returnType = methodInfo.ReturnType.GetTypeInfo();
    //     return returnType.IsTaskWithResult();
    // }

    internal static bool IsReturnValueTask(this MethodInfo methodInfo)
    {
        if (methodInfo == null)
        {
            throw new ArgumentNullException(nameof(methodInfo));
        }

        var returnType = methodInfo.ReturnType.GetTypeInfo();
        return returnType.IsValueTaskWithResult();
    }

    internal static bool IsVisibleAndVirtual(this PropertyInfo property)
    {
        if (property == null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        return (property.CanRead && property.GetMethod.IsVisibleAndVirtual()) ||
               (property.CanWrite && property.GetMethod.IsVisibleAndVirtual());
    }

    internal static bool IsVisibleAndVirtual(this MethodInfo method)
    {
        if (method == null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        if (method.IsStatic || method.IsFinal)
        {
            return false;
        }

        return method.IsVirtual && (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly);
    }

    internal static MethodInfo GetMethodBySignature(this TypeInfo typeInfo, MethodInfo method)
    {
        if (typeInfo == null)
        {
            throw new ArgumentNullException(nameof(typeInfo));
        }

        if (method == null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        var methods = typeInfo.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var displayName = method.GetReflector().DisplayName;
        var invocation = methods.FirstOrDefault(x => x.GetReflector().DisplayName.Equals(displayName, StringComparison.Ordinal));
        if (invocation != null)
        {
            return invocation;
        }

        var declaringType = method.DeclaringType;
        displayName = $"{declaringType.GetReflector().FullDisplayName}.{displayName.Split(' ').Last()}";
        invocation = methods.FirstOrDefault(x => x.GetReflector().DisplayName.Split(' ').Last().Equals(displayName, StringComparison.Ordinal));
        if (invocation != null)
        {
            return invocation;
        }

        invocation = typeInfo.GetMethodBySignature(new MethodSignature(method));
        if (invocation != null)
        {
            return invocation;
        }

        displayName = $"{declaringType.GetReflector().FullDisplayName}.{method.Name}";
        return typeInfo.GetMethodBySignature(
            new MethodSignature(method, displayName));
    }
}