using Kurisu.RemoteCall.Abstractions;

namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 请求header定义
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
public class RequestHeaderAttribute : Attribute
{
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
    /// header name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// header value
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// header处理器
    /// </summary>
    public Type Handler { get; }
}