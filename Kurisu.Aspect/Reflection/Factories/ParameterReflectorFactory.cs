using System.Reflection;
using Kurisu.Aspect.Reflection.Internals;

namespace Kurisu.Aspect.Reflection.Factories;

internal static class ParameterReflectorFactory
{
    internal static ParameterReflector Create(ParameterInfo parameterInfo)
    {
        if (parameterInfo == null)
        {
            throw new ArgumentNullException(nameof(parameterInfo));
        } 

        return ReflectorCacheUtils<ParameterInfo, ParameterReflector>.GetOrAdd(parameterInfo, info => new ParameterReflector(info));
    }
}