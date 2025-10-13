namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 参数指定为route值
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class RequestRouteAttribute : Attribute
{
}