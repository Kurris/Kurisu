using System.Net;
using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Utils;

namespace Kurisu.RemoteCall.Default;

internal class DefaultRemoteCallStandardResponseResultHandler : IRemoteCallResponseResultHandler
{
    private readonly IJsonSerializer _jsonSerializer;

    public DefaultRemoteCallStandardResponseResultHandler(IJsonSerializer jsonSerializer)
    {
        _jsonSerializer = jsonSerializer;
    }

    /// <inheritdoc />
    public TResult Handle<TResult>(HttpStatusCode statusCode, string responseBody)
    {
        var apiResult = _jsonSerializer.Deserialize<ApiResult<TResult>>(responseBody);
        var isSuccessStatusCode = (int)statusCode >= 200 && (int)statusCode <= 299;

        if (!isSuccessStatusCode || apiResult.Code != 200)
        {
            if (!string.IsNullOrEmpty(apiResult?.Msg))
            {
                throw new Exception("远程调用异常:" + apiResult.Msg);
            }

            throw new HttpRequestException(statusCode + " " + (int)statusCode);
        }

        return apiResult.Data;
    }

    internal sealed class ApiResult<T>
    {
        /// <summary>
        /// 状态
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// 信息
        /// </summary>
        public string Msg { get; set; }

        /// <summary>
        /// 结果内容
        /// </summary>
        public T Data { get; set; }
    }
}