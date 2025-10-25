using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Utils;

namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 请求content内容处理
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
public sealed class RequestContentAttribute : Attribute
{
    /// <summary>
    /// handler  <see cref="IRemoteCallContentHandler"/>
    /// </summary>
    /// <param name="handler">内容处理器</param>
    public RequestContentAttribute(Type handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        if (!handler.IsInheritedFrom<IRemoteCallContentHandler>()) throw new ArgumentException(nameof(handler) + " 必须继承自 " + nameof(IRemoteCallContentHandler));
        Handler = handler;
    }

    /// <summary>
    /// 处理类
    /// </summary>
    public Type Handler { get; }
}