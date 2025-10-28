using System.Net;
using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Utils;

namespace Kurisu.RemoteCall.Default;

internal class DefaultRemoteCallResponseResultHandler : IRemoteCallResponseResultHandler
{
    private readonly IJsonSerializer _jsonSerializer;
    private readonly ICommonUtils _commonUtils;

    public DefaultRemoteCallResponseResultHandler(IJsonSerializer jsonSerializer , ICommonUtils commonUtils)
    {
        _jsonSerializer = jsonSerializer;
        _commonUtils = commonUtils;
    }

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

    private TResult Handle<TResult>(string responseBody)
    {
        var type = typeof(TResult);
        if (_commonUtils.IsReferenceType(type))
        {
            return _jsonSerializer.Deserialize<TResult>(responseBody);
        }

        return (TResult)Convert.ChangeType(responseBody, type);
    }
}