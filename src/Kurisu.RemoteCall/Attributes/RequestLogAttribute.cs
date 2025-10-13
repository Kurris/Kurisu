namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 请求日志输出
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
public sealed class RequestLogAttribute : Attribute
{
    /// <summary>
    /// init
    /// </summary>
    public RequestLogAttribute()
    {
    }
}