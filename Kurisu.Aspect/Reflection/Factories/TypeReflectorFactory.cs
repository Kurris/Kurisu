using System.Reflection;
using Kurisu.Aspect.Reflection.Internals;

namespace Kurisu.Aspect.Reflection.Factories;

internal static class TypeReflectorFactory
{
    internal static TypeReflector Create(TypeInfo typeInfo)
    {
        if (typeInfo == null)
        {
            throw new ArgumentNullException(nameof(typeInfo));
        }

        return ReflectorCacheUtils<TypeInfo, TypeReflector>.GetOrAdd(typeInfo, info => new TypeReflector(info));
    }
}