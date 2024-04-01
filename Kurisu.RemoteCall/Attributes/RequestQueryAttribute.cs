namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 参数转换为 url query
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class RequestQueryAttribute : Attribute
{
}