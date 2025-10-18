using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Kurisu.AspNetCore.Abstractions.UnifyResultAndValidation;
using Kurisu.AspNetCore.CustomClass;
using Kurisu.AspNetCore.Utils.Extensions;

namespace Kurisu.AspNetCore.UnifyResultAndValidation;

/// <summary>
/// 默认Api结果结构
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
[SkipScan]
public sealed class ApiResult<T> : IApiResult
{
    /// <summary>
    /// 数据结果返回模型
    /// <code>
    /// Message = null;
    /// Status = Status.Error;
    /// Data = Default(T)
    /// </code>
    /// </summary>
    public ApiResult()
    {
        Msg = "执行异常";
        Data = default;
        Code = ApiStateCode.Error;
    }

    /// <summary>
    /// 数据结果返回模型
    /// </summary>
    /// <param name="msg">信息</param>
    /// <param name="data">数据</param>
    /// <param name="state">状态</param>
    public ApiResult(string msg, T data, ApiStateCode state)
    {
        Msg = msg;
        Data = data;
        Code = state;
    }

    /// <summary>
    /// 信息
    /// </summary>
    public string Msg { get; set; }

    /// <summary>
    /// 结果内容
    /// </summary>
    public T Data { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public ApiStateCode Code { get; set; }

    /// <summary>
    ///  尝试获取数据
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    /// <returns></returns>
    public bool TryGetData<TResponse>(out TResponse data)
    {
        data = default;
        if (Data is not TResponse response) return false;
        
        data = response;
        return true;

    }

    /// <summary>
    /// 成功结果
    /// </summary>
    /// <param name="apiResult"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public IApiResult GetDefaultSuccessApiResult<TResult>(TResult apiResult)
    {
        return new ApiResult<TResult>
        {
            Code = ApiStateCode.Success,
            Msg = "操作成功",
            Data = apiResult
        };
    }

    /// <summary>
    /// 校验结果
    /// </summary>
    /// <param name="validateMessage"></param>
    /// <returns></returns>
    public IApiResult GetDefaultValidateApiResult(string validateMessage)
    {
        return new ApiResult<object>
        {
            Code = ApiStateCode.ValidateError,
            Msg = validateMessage,
        };
    }

    /// <summary>
    /// 校验结果
    /// </summary>
    /// <param name="validateMessage"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public IApiResult GetDefaultValidateApiResult(string validateMessage, object data)
    {
        return new ApiResult<object>
        {
            Code = ApiStateCode.ValidateError,
            Msg = validateMessage,
            Data = data
        };
    }

    /// <summary>
    /// 无权操作结果
    /// </summary>
    /// <returns></returns>
    public IApiResult GetDefaultForbiddenApiResult()
    {
        return new ApiResult<object>
        {
            Code = ApiStateCode.Forbidden,
            Msg = "无权操作"
        };
    }

    /// <summary>
    /// 错误结果
    /// </summary>
    /// <param name="errorMessage"></param>
    /// <returns></returns>
    public IApiResult GetDefaultErrorApiResult(string errorMessage)
    {
        return new ApiResult<object>
        {
            Code = ApiStateCode.Error,
            Msg = errorMessage
        };
    }
}

/// <summary>
/// 默认Api结果结构
/// </summary>
public class DefaultApiResult
{
    /// <summary>
    /// 成功
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static IApiResult Success<T>(T data)
    {
        return new ApiResult<T>
        {
            Msg = ApiStateCode.Success.GetDisplay(),
            Data = data,
            Code = ApiStateCode.Success
        };
    }

    /// <summary>
    /// 异常
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    public static IApiResult Error(string msg)
    {
        return new ApiResult<object>
        {
            Msg = msg,
            Data = null,
            Code = ApiStateCode.Error
        };
    }

    /// <summary>
    /// 未授权
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    public static IApiResult Unauthorized(string msg)
    {
        return new ApiResult<object>
        {
            Msg = msg,
            Data = null,
            Code = ApiStateCode.Unauthorized
        };
    }

    /// <summary>
    /// 无权操作
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    public static IApiResult Forbidden(string msg)
    {
        return new ApiResult<object>
        {
            Msg = msg,
            Data = null,
            Code = ApiStateCode.Forbidden
        };
    }

    /// <summary>
    /// 404
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    public static IApiResult NotFound(string msg)
    {
        return new ApiResult<object>
        {
            Msg = msg,
            Data = null,
            Code = ApiStateCode.NotFound
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
    [Lang("操作成功")]
    Success = 200,

    /// <summary>
    /// 鉴权失败
    /// </summary>
    [Lang("鉴权失败")]
    Unauthorized = 401,

    /// <summary>
    /// 无权操作
    /// </summary>
    [Lang("无权操作")]
    Forbidden = 403,

    /// <summary>
    /// 资源不存在
    /// </summary>
    [Lang("资源不存在")]
    NotFound = 404,

    /// <summary>
    /// 请求参数有误
    /// </summary>
    [Lang("请求参数有误")]
    ValidateError = 400,

    /// <summary>
    /// 执行异常
    /// </summary>
    [Lang("执行异常")]
    Error = 500
}