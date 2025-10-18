using Kurisu.AspNetCore.Abstractions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.UnifyResultAndValidation;

/// <summary>
/// api返回值接口
/// </summary>
[SkipScan]
public interface IApiResult
{
    /// <summary>
    /// 尝试获取数据
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    /// <returns></returns>
    bool TryGetData<TResponse>(out TResponse data);

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
    /// 获取默认验证错误结果
    /// </summary>
    /// <param name="validateMessage"></param>
    /// <returns></returns>
    IApiResult GetDefaultValidateApiResult(string validateMessage);

    /// <summary>
    /// 获取默认验证错误结果
    /// </summary>
    /// <param name="validateMessage"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    IApiResult GetDefaultValidateApiResult(string validateMessage, object data);
}