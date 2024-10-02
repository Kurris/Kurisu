using System.ComponentModel;
using Kurisu.AspNetCore.CustomClass;
using Kurisu.AspNetCore.UnifyResultAndValidation.Abstractions;
using Kurisu.AspNetCore.Utils.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Kurisu.AspNetCore.UnifyResultAndValidation;

/// <summary>
/// 默认Api结果结构
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
[SkipScan]
public class DefaultApiResult<T> : IApiResult
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


    /// <inheritdoc/>
    public virtual IApiResult GetDefaultSuccessApiResult<TResult>(TResult apiResult)
    {
        return new DefaultApiResult<TResult>
        {
            Code = ApiStateCode.Success,
            Msg = "操作成功",
            Data = apiResult
        };
    }

    /// <inheritdoc/>
    public virtual IApiResult GetDefaultValidateApiResult(string validateMessage)
    {
        return new DefaultApiResult<object>
        {
            Code = ApiStateCode.ValidateError,
            Msg = validateMessage,
        };
    }

    /// <inheritdoc/>
    public virtual IApiResult GetDefaultForbiddenApiResult()
    {
        return new DefaultApiResult<object>
        {
            Code = ApiStateCode.Forbidden,
            Msg = "无权操作"
        };
    }

    /// <inheritdoc/>
    public virtual IApiResult GetDefaultErrorApiResult(string errorMessage)
    {
        return new DefaultApiResult<object>
        {
            Code = ApiStateCode.Error,
            Msg = errorMessage
        };
    }

    /// <summary>
    /// 确保成功状态
    /// </summary>
    /// <returns></returns>
    /// <exception cref="UserFriendlyException"></exception>
    public T EnsureSuccessStatusCode()
    {
        if (Code != ApiStateCode.Success)
        {
            throw new UserFriendlyException(Msg);
        }

        return Data;
    }
}

/// <summary>
/// 默认Api结果结构
/// </summary>
public class DefaultApiResult : DefaultApiResult<object>
{
    /// <summary>
    /// 成功
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static DefaultApiResult Success(object data)
    {
        return new DefaultApiResult
        {
            Msg = ApiStateCode.Success.GetDescription(),
            Data = data,
            Code = ApiStateCode.Success
        };
    }

    /// <summary>
    /// 异常
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    public static DefaultApiResult Error(string msg)
    {
        return new DefaultApiResult
        {
            Msg = msg,
            Data = null,
            Code = ApiStateCode.Error
        };
    }
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