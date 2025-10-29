namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 禁用远程调用请求/响应的日志输出。
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
public sealed class RequestDisableLogAttribute : Attribute;