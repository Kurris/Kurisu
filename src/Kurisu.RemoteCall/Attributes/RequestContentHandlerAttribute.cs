using Kurisu.RemoteCall.Abstractions;

namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 请求content内容处理
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
public sealed class RequestContentHandlerAttribute : Attribute
{
    /// <summary>
    /// contentHandler  <see cref="IRemoteCallContentHandler"/>
    /// </summary>
    /// <param name="contentHandler">内容处理器</param>
    public RequestContentHandlerAttribute(Type contentHandler)
    {
        ContentHandler = contentHandler;
    }

    /// <summary>
    /// 处理类
    /// </summary>
    public Type ContentHandler { get; }
}