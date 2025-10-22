using Kurisu.RemoteCall.Abstractions;

namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 请求content内容处理
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
public sealed class RequestContentHandlerAttribute : Attribute
{
    /// <summary>
    /// handler  <see cref="IRemoteCallContentHandler"/>
    /// </summary>
    /// <param name="handler">内容处理器</param>
    public RequestContentHandlerAttribute(Type handler)
    {
        Handler = handler;
    }

    /// <summary>
    /// 处理类
    /// </summary>
    public Type Handler { get; }
}