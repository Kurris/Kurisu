using System.Net;

namespace Kurisu.RemoteCall.Abstractions;

/// <summary>
/// 结果处理器
/// </summary>
public interface IRemoteCallResponseResultHandler : IRemoteCallHandler
{
    /// <summary>
    /// Handle
    /// </summary>
    /// <param name="statusCode">状态码</param>
    /// <param name="responseBody">请求响应报文</param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public TResult Handle<TResult>(HttpStatusCode statusCode, string responseBody);
}