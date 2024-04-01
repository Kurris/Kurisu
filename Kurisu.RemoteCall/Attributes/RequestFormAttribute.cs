namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 参数转换为 form body
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class RequestFormAttribute : Attribute
{
}