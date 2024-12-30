using System.Reflection;
using Kurisu.Aspect.Reflection.Internals;
using Kurisu.Aspect.Reflection.Reflectors;

namespace Kurisu.Aspect.Reflection.Factories;

internal static class MethodReflectorFactory
{
    public static MethodReflector Create(MethodInfo reflectionInfo, CallOptions callOption)
    {
        if (reflectionInfo == null)
        {
            throw new ArgumentNullException(nameof(reflectionInfo));
        }

        return ReflectorCacheUtils<Tuple<MethodInfo, CallOptions>, MethodReflector>.GetOrAdd(Tuple.Create(reflectionInfo, callOption), item =>
        {
            var methodInfo = item.Item1;
            if (methodInfo.ContainsGenericParameters)
            {
                return new MethodOpenGenericReflector(methodInfo);
            }

            if (methodInfo.IsStatic)
            {
                return new MethodStaticReflector(methodInfo);
            }

            if (methodInfo.DeclaringType!.GetTypeInfo().IsValueType || callOption == CallOptions.Call)
            {
                return new MethodCallReflector(methodInfo);
            }

            return new MethodReflector(methodInfo);
        });
    }
}