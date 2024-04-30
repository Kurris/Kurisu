using System.Reflection;
using Kurisu.Aspect.Reflection.Internals;

namespace Kurisu.Aspect.Reflection.Factories;

internal static class PropertyReflectorFactory
{
    public static PropertyReflector Create(PropertyInfo reflectionInfo, CallOptions callOption)
    {
        if (reflectionInfo == null)
        {
            throw new ArgumentNullException(nameof(reflectionInfo));
        }

        return ReflectorCacheUtils<Tuple<PropertyInfo, CallOptions>, PropertyReflector>.GetOrAdd(Tuple.Create(reflectionInfo, callOption), item =>
        {
            var property = item.Item1;
            if (property.DeclaringType!.GetTypeInfo().ContainsGenericParameters)
            {
                return new PropertyReflector.OpenGenericPropertyReflector(property);
            }

            if ((property.CanRead && property.GetMethod!.IsStatic) || (property.CanWrite && property.SetMethod!.IsStatic))
            {
                return new PropertyReflector.StaticPropertyReflector(property);
            }

            if (property.DeclaringType.GetTypeInfo().IsValueType || item.Item2 == CallOptions.Call)
            {
                return new PropertyReflector.CallPropertyReflector(property);
            }

            return new PropertyReflector(property);
        });
    }
}