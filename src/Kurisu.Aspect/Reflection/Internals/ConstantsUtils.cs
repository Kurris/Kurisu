using System.Reflection;
using Kurisu.Aspect.Reflection.Extensions;

namespace Kurisu.Aspect.Reflection.Internals;

internal static class MethodInfoConstant
{
    internal static readonly MethodInfo GetTypeFromHandle = InternalExtensions.GetMethod<Func<RuntimeTypeHandle, Type>>(handle => Type.GetTypeFromHandle(handle));
    
    internal static readonly MethodInfo GetMethodFromHandle = InternalExtensions.GetMethod<Func<RuntimeMethodHandle, RuntimeTypeHandle, MethodBase>>((h1, h2) => MethodBase.GetMethodFromHandle(h1, h2));
}