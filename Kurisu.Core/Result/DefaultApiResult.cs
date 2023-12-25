using Kurisu.Core.Result.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Core.Result;

/// <summary>
/// 数据结果返回模型
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
        Code = ApiStateCode.Error;
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
    public string Msg { get; set; }

    /// <summary>
    /// 结果内容
    /// </summary>
    public T Data { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public ApiStateCode Code { get; set; }


    public virtual IApiResult GetDefaultSuccessApiResult<TResult>(TResult apiResult)
    {
        return new DefaultApiResult<TResult>
        {
            Code = ApiStateCode.Success,
            Msg = "success",
            Data = apiResult
        };
    }

    public virtual IApiResult GetDefaultValidateApiResult<TResult>(TResult apiResult)
    {
        return new DefaultApiResult<TResult>
        {
            Code = ApiStateCode.ValidateError,
            Msg = "validation error",
            Data = apiResult
        };
    }

    public virtual IApiResult GetDefaultForbiddenApiResult()
    {
        return new DefaultApiResult<object>
        {
            Code = ApiStateCode.Forbidden,
            Msg = "forbidden"
        };
    }

    public virtual IApiResult GetDefaultErrorApiResult(string errorMessage)
    {
        return new DefaultApiResult<object>
        {
            Msg = errorMessage
        };
    }
}

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
    Success = 200,

    /// <summary>
    /// 鉴权失败
    /// </summary>
    Unauthorized = 401,

    /// <summary>
    /// 无权限
    /// </summary>
    Forbidden = 403,

    /// <summary>
    /// 找不到资源
    /// </summary>
    NotFound = 404,

    /// <summary>
    /// 实体验证失败
    /// </summary>
    ValidateError = 400,

    /// <summary>
    /// 执行异常
    /// </summary>
    Error = 500
}