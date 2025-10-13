namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 忽略参数为query
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class RequestIgnoreQueryAttribute : Attribute
{
}