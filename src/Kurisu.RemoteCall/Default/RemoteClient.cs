using Kurisu.RemoteCall.Abstractions;

namespace Kurisu.RemoteCall.Default;

public class RemoteClient
{
    /// <summary>
    /// client name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// BaseUrl
    /// </summary>
    public string BaseUrl { get; set; }

    /// <summary>
    /// 请求处理策略<see cref="IRemoteCallPolicyHandler"/>
    /// </summary>
    public Type PolicyHandler { get; set; }
}