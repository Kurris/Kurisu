using System.Collections.Concurrent;
using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Default;

namespace Kurisu.RemoteCall.Utils;

/// <summary>
/// 处理器缓存
/// </summary>
internal static class HandlerCache
{
    internal static readonly ConcurrentBag<string> ConfigClients = new();

    internal static readonly ConcurrentDictionary<Type, IRemoteCallResultHandler> ResultHandlers = new()
    {
        [typeof(RemoteCallStandardResultHandler)] = new DefaultRemoteCallResultHandler()
    };

    internal static readonly ConcurrentDictionary<Type, IRemoteCallContentHandler> ContentHandlers = new();
}