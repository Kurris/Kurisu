using Kurisu.RemoteCall.Abstractions;

namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 请求header定义
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
public class RequestHeaderAttribute : Attribute
{
    public string Name { get; }
    public string Value { get; }

    /// <summary>
    /// 请求header定义
    /// </summary>
    /// <param name="name">header name</param>
    /// <param name="value">值</param>
    public RequestHeaderAttribute(string name, string value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>
    /// 请求header定义
    /// </summary>
    /// <param name="handler"><see cref="IRemoteCallHeaderHandler"/>></param>
    public RequestHeaderAttribute(Type handler)
    {
        Handler = handler;
    }

    /// <summary>
    /// header处理器
    /// </summary>
    public Type Handler { get; }
}