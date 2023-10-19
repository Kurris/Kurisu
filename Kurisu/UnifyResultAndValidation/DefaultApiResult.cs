using Kurisu.UnifyResultAndValidation.Abstractions;
using Microsoft.Extensions.DependencyInjection;
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

namespace Kurisu.UnifyResultAndValidation;

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
        this.Code = ApiStateCode.Error;
    }

    /// <summary>
    /// 数据结果返回模型
    /// </summary>
    /// <param name="message">信息</param>
    /// <param name="data">数据</param>
    /// <param name="state">状态</param>
    public DefaultApiResult(string message, T data, ApiStateCode state)
    {
        this.Message = message;
        this.Data = data;
        this.Code = state;
    }

    /// <summary>
    /// 信息
    /// </summary>
    public string Message { get; set; }

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
            Message = "success",
            Data = apiResult
        };
    }

    public virtual IApiResult GetDefaultValidateApiResult<TResult>(TResult apiResult)
    {
        return new DefaultApiResult<TResult>
        {
            Code = ApiStateCode.ValidateError,
            Message = "validation error",
            Data = apiResult
        };
    }

    public virtual IApiResult GetDefaultForbiddenApiResult()
    {
        return new DefaultApiResult<object>
        {
            Code = ApiStateCode.Forbidden,
            Message = "forbidden"
        };
    }

    public virtual IApiResult GetDefaultErrorApiResult(string errorMessage)
    {
        return new DefaultApiResult<object>
        {
            Message = errorMessage
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