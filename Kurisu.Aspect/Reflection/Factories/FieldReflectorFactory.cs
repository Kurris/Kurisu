using System.Reflection;
using Kurisu.Aspect.Reflection.Internals;

namespace Kurisu.Aspect.Reflection.Factories;

internal static class FieldReflectorFactory
{
    public static FieldReflector Create(FieldInfo reflectionInfo)
    {
        if (reflectionInfo == null)
        {
            throw new ArgumentNullException(nameof(reflectionInfo));
        }

        return ReflectorCacheUtils<FieldInfo, FieldReflector>.GetOrAdd(reflectionInfo, field =>
        {
            if (field.DeclaringType!.GetTypeInfo().ContainsGenericParameters)
            {
                return new FieldOpenGenericReflector(field);
            }

            if (field.DeclaringType.IsEnum)
            {
                return new FieldEnumReflector(field);
            }

            return field.IsStatic ? new FieldStaticReflector(field) : new FieldReflector(field);
        });
    }
}