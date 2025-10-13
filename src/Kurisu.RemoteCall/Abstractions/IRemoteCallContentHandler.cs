using System.Reflection;

namespace Kurisu.RemoteCall.Abstractions;

/// <summary>
/// 请求content处理
/// </summary>
public interface IRemoteCallContentHandler : IRemoteCallHandler
{
    HttpContent Create(ContentInfo contentInfo);
}

public class ContentInfo
{
    public MethodInfo Method { get; set; }
    public List<ParameterValue> Values { get; set; }
}