namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 参数指定为url query
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class RequestQueryAttribute : Attribute
{
}