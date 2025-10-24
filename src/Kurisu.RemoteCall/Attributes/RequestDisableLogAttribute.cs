namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 禁止请求日志输出
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
public sealed class RequestDisableLogAttribute : Attribute;