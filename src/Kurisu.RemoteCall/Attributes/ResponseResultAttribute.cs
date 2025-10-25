using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Default;
using Kurisu.RemoteCall.Utils;

namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 响应处理器
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
public sealed class ResponseResultAttribute : Attribute
{
    /// <summary>
    /// 响应处理器handler,使用<see cref="RemoteCallStandardResultHandler"/>
    /// </summary>
    public ResponseResultAttribute() : this(typeof(RemoteCallStandardResultHandler))
    {
    }

    /// <summary>
    /// 响应处理器handler
    /// </summary>
    /// <param name="handlerType"><see cref="IRemoteCallResultHandler"/></param>
    public ResponseResultAttribute(Type handlerType)
    {
        if (handlerType is null) throw new ArgumentException(nameof(handlerType));
        if (!handlerType.IsInheritedFrom<IRemoteCallResultHandler>()) throw new ArgumentException(nameof(handlerType) + " 必须继承自 " + nameof(IRemoteCallResultHandler));

        Handler = handlerType;
    }

    /// <summary>
    /// 处理类
    /// </summary>
    public Type Handler { get; }
}