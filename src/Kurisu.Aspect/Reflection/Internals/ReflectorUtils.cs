using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Kurisu.Aspect.Reflection.Internals;

internal static class ReflectorCacheUtils<TMemberInfo, TReflector>
{
    private static readonly ConcurrentDictionary<TMemberInfo, TReflector> _dictionary = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static TReflector GetOrAdd(TMemberInfo key, Func<TMemberInfo, TReflector> factory)
    {
        return _dictionary.GetOrAdd(key, factory);
    }
}