namespace Kurisu.RemoteCall.Proxy.Enums;

/// <summary>
/// 代理方法枚举类型
/// </summary>
internal enum MethodType
{
    /// <summary>
    /// 同步
    /// </summary>
    Synchronous,

    /// <summary>
    /// 异步
    /// </summary>
    AsyncAction,

    /// <summary>
    /// 异步返回值
    /// </summary>
    AsyncFunction,
}
