using System.Collections.Concurrent;
using System.Reflection;

namespace Kurisu.RemoteCall.Utils;

/// <summary>
/// 处理器缓存
/// </summary>
internal static class HandlerCache
{
    internal static readonly ConcurrentDictionary<(Type, string, string), MethodInfo> Methods = new();
}