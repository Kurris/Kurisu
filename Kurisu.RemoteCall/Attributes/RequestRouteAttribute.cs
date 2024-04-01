namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 参数转换为 form body
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class RequestRouteAttribute : Attribute
{
}