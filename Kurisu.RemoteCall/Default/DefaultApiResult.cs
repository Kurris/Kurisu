using System.ComponentModel;
using Newtonsoft.Json;

namespace Kurisu.RemoteCall.Default;

/// <summary>
/// 默认Api结果结构
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class DefaultApiResult<T>
{
    /// <summary>
    /// 数据结果返回模型
    /// <code>
    /// Message = null;
    /// Status = Status.Error;
    /// Data = Default(T)
    /// </code>
    /// </summary>
    public DefaultApiResult()
    {
    }

    /// <summary>
    /// 数据结果返回模型
    /// </summary>
    /// <param name="msg">信息</param>
    /// <param name="data">数据</param>
    /// <param name="state">状态</param>
    public DefaultApiResult(string msg, T data, ApiStateCode state)
    {
        Msg = msg;
        Data = data;
        Code = state;
    }

    /// <summary>
    /// 信息
    /// </summary>
    [JsonProperty(Order = 1)]
    public string Msg { get; set; }

    /// <summary>
    /// 结果内容
    /// </summary>
    [JsonProperty(Order = 2)]
    public T Data { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [JsonProperty(Order = 0)]
    public ApiStateCode Code { get; set; }

  

    /// <summary>
    /// 确保成功状态
    /// </summary>
    /// <returns></returns>
    public T EnsureSuccessStatusCode()
    {
        if (Code != ApiStateCode.Success)
        {
            throw new Exception(Msg);
        }

        return Data;
    }
}

/// <summary>
/// 默认Api结果结构
/// </summary>
public class DefaultApiResult : DefaultApiResult<object>
{
}

/// <summary>
/// 返回状态
/// </summary>
public enum ApiStateCode
{
    /// <summary>
    /// 操作成功
    /// </summary>
    [Description("操作成功")]
    Success = 200,

    /// <summary>
    /// 鉴权失败
    /// </summary>
    [Description("鉴权失败")]
    Unauthorized = 401,

    /// <summary>
    /// 无权操作
    /// </summary>
    [Description("无权操作")]
    Forbidden = 403,

    /// <summary>
    /// 资源不存在
    /// </summary>
    [Description("资源不存在")]
    NotFound = 404,

    /// <summary>
    /// 请求参数有误
    /// </summary>
    [Description("请求参数有误")]
    ValidateError = 400,

    /// <summary>
    /// 执行异常
    /// </summary>
    [Description("执行异常")]
    Error = 500
}