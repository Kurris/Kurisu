namespace Kurisu.RemoteCall.Abstractions;

/// <summary>
/// Url处理器
/// </summary>
public interface IRemoteCallUrlResolver
{
    /// <summary>
    /// 获取完整Url
    /// </summary>
    /// <param name="httpMethod"></param>
    /// <param name="baseUrl"></param>
    /// <param name="template"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    string GetUrl(HttpMethodType httpMethod, string baseUrl, string template, List<ParameterValue> parameters);
}