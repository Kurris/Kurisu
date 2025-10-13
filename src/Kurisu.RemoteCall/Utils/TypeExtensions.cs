using Kurisu.RemoteCall.Proxy.Abstractions;
using System.Reflection;

namespace Kurisu.RemoteCall.Utils;

/// <summary>
/// 类型扩展类
/// </summary>
internal static class TypeExtensions
{
    internal static T GetCustomAttribute<T>(this IProxyInvocation proxyInvocation) where T : Attribute
    {
        return proxyInvocation.Method.GetCustomAttribute<T>() ?? proxyInvocation.InterfaceType.GetCustomAttribute<T>();
    }

    internal static bool IsImplementFrom<T>(this Type type) where T : class
    {
        return type != null && type.IsAssignableTo(typeof(T));
    }
}