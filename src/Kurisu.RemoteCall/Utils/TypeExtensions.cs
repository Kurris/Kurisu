using System.Reflection;
using Kurisu.RemoteCall.Proxy.Abstractions;

namespace Kurisu.RemoteCall.Utils;

/// <summary>
/// 类型扩展类
/// </summary>
internal static class TypeExtensions
{
    public static bool TryGetCustomAttribute<T>(this IProxyInvocation proxyInvocation, out T attribute) where T : Attribute
    {
        attribute = proxyInvocation.Method.GetCustomAttribute<T>();
        if (attribute != null)
        {
            return true;
        }

        attribute = proxyInvocation.InterfaceType.GetCustomAttribute<T>();
        return attribute != null;
    }

    /// <summary>
    /// 是否继承自某个类型(已做null检查)
    /// </summary>
    /// <param name="type"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    internal static bool IsInheritedFrom<T>(this Type type) where T : class
    {
        return type != null && type.IsAssignableTo(typeof(T));
    }
}