using Microsoft.Extensions.Logging;

namespace Kurisu.RemoteCall.Abstractions;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TResult"></typeparam>
public interface IRemoteCallInterceptor<TResult>
{
    /// <summary>
    /// 验证参数
    /// </summary>
    /// <param name="parameters"></param>
    void ValidateParameters(object[] parameters);

    /// <summary>
    /// 
    /// </summary>>
    /// <param name="httpMethod"></param>
    /// <param name="baseUrl"></param>
    /// <param name="template"></param>
    /// <param name="wrapParameterValues"></param>
    /// <returns></returns>
    string ResolveUrl(string httpMethod, string baseUrl, string template, List<ParameterValue> wrapParameterValues);


    /// <summary>
    /// 发送请求前拦截
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    Task BeforeRequestAsync(HttpClient httpClient, HttpRequestMessage request);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    Task UseAuthAsync(HttpRequestMessage request);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
    Task<TResult> AfterResponseAsync(HttpResponseMessage response);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="exception"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    bool TryOnException(Exception exception, out TResult result);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="httpMethod"></param>
    /// <param name="url"></param>
    void Log(ILogger logger, string httpMethod, string url);
}