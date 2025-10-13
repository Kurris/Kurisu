namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 参数定义为form body
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class RequestFormAttribute : Attribute
{
}