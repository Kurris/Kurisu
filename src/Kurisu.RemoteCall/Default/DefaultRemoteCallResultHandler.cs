using System.Net;
using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Utils;
using Newtonsoft.Json;

namespace Kurisu.RemoteCall.Default;

/// <summary>
/// 默认结果处理器
/// </summary>
internal class DefaultRemoteCallResultHandler : IRemoteCallResultHandler
{
    public TResult Handle<TResult>(HttpStatusCode statusCode, string responseBody)
    {
        var result = Handle<TResult>(responseBody);
        var isSuccessStatusCode = (int)statusCode >= 200 && (int)statusCode <= 299;
        if (!isSuccessStatusCode)
        {
            if (!string.IsNullOrEmpty(responseBody))
            {
                throw new Exception("远程调用异常:" + responseBody);
            }

            throw new HttpRequestException(statusCode + " " + (int)statusCode);
        }

        return result;
    }

    private static TResult Handle<TResult>(string responseBody)
    {
        var type = typeof(TResult);
        if (Common.IsReferenceType(type))
        {
            return JsonConvert.DeserializeObject<TResult>(responseBody);
        }

        return (TResult)Convert.ChangeType(responseBody, type);
    }
}