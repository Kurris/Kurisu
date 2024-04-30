using System.Reflection;
using Kurisu.Aspect.Reflection.Internals;
using Kurisu.Aspect.Reflection.Reflectors;

namespace Kurisu.Aspect.Reflection.Factories;

internal static class ConstructorReflectorFactory
{
    internal static ConstructorReflector Create(ConstructorInfo constructorInfo)
    {
        if (constructorInfo == null)
        {
            throw new ArgumentNullException(nameof(constructorInfo));
        }

        return ReflectorCacheUtils<ConstructorInfo, ConstructorReflector>.GetOrAdd(constructorInfo, info =>
            info.DeclaringType!.GetTypeInfo().ContainsGenericParameters
                ? new ConstructorOpenGenericReflector(info)
                : new ConstructorReflector(info));
    }
}