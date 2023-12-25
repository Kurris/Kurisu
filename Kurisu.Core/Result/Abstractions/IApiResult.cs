using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Core.Result.Abstractions;

/// <summary>
/// api返回值接口
/// </summary>
[SkipScan]
public interface IApiResult
{
    /// <summary>
    /// 获取默认成功结果
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="apiResult"></param>
    /// <returns></returns>
    IApiResult GetDefaultSuccessApiResult<TResult>(TResult apiResult);

    /// <summary>
    /// 获取默认错误结果
    /// </summary>
    /// <param name="errorMessage"></param>
    /// <returns></returns>
    IApiResult GetDefaultErrorApiResult(string errorMessage);

    /// <summary>
    /// 获取默认无权访问的结果403
    /// </summary>
    /// <returns></returns>
    IApiResult GetDefaultForbiddenApiResult();

    /// <summary>
    /// 获取默认验证错误结果400
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="apiResult"></param>
    /// <returns></returns>
    IApiResult GetDefaultValidateApiResult<TResult>(TResult apiResult);
}