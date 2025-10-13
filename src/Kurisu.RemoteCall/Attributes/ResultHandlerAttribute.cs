using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Default;

namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 响应处理器
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
public sealed class ResultHandlerAttribute : Attribute
{
    /// <summary>
    /// 响应处理器handler,使用<see cref="RemoteCallStandardResultHandler"/>
    /// </summary>
    public ResultHandlerAttribute()
    {
        Handler = typeof(RemoteCallStandardResultHandler);
    }

    /// <summary>
    /// 响应处理器handler
    /// </summary>
    /// <param name="handlerType"><see cref="IRemoteCallResultHandler"/></param>
    public ResultHandlerAttribute(Type handlerType)
    {
        Handler = handlerType;
    }

    /// <summary>
    /// 处理类
    /// </summary>
    public Type Handler { get; }
}