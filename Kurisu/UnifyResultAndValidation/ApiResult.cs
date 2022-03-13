using System;
using Kurisu.UnifyResultAndValidation.Abstractions;

namespace Kurisu.UnifyResultAndValidation
{
    /// <summary>
    /// 数据结果返回模型
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public class ApiResult<T> : IApiResult
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
            this.Code = Status.Fail;
        }

        /// <summary>
        /// 数据结果返回模型
        /// </summary>
        /// <param name="message">信息</param>
        /// <param name="data">数据</param>
        /// <param name="status">状态</param>
        public ApiResult(string message, T data, Status status)
        {
            this.Message = message;
            this.Data = data;
            this.Code = status;
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
        public Status Code { get; set; }


        public IApiResult GetDefaultSuccessApiResult<TResult>(TResult apiResult)
        {
            return new ApiResult<TResult>()
            {
                Code = Status.Success,
                Message = "操作成功",
                Data = apiResult
            };
        }

        public IApiResult GetDefaultValidateApiResult<TResult>(TResult apiResult)
        {
            return new ApiResult<TResult>()
            {
                Code = Status.ValidateError,
                Message = "实体验证失败",
                Data = apiResult
            };
        }

        public IApiResult GetDefaultForbiddenApiResult()
        {
            return new ApiResult<object>()
            {
                Code = Status.Forbidden,
                Message = "无权访问"
            };
        }

        public IApiResult GetDefaultErrorApiResult(string errorMessage)
        {
            return new ApiResult<object>()
            {
                Code = Status.Error,
                Message = errorMessage
            };
        }
    }

    /// <summary>
    /// 返回状态
    /// </summary>
    public enum Status
    {
        /// <summary>
        /// 操作成功
        /// </summary>
        Success = 200,

        /// <summary>
        /// 操作失败
        /// </summary>
        Fail = 202,

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
        Error = 500,
    }


    /// <summary>
    /// ApiResult扩展方法
    /// </summary>
    public static class ApiResultExtensions
    {
        public static ApiResult<T> Success<T>(this T data)
        {
            return new ApiResult<T>
            {
                Code = Status.Success,
                Message = "操作成功",
                Data = data
            };
        }


        /// <summary>
        /// 设置异常状态
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static ApiResult<T> Error<T>(Exception ex)
        {
            return new ApiResult<T>
            {
                Code = Status.Error,
                Message = ex.GetBaseException().Message
            };
        }

        /// <summary>
        /// 设置异常状态
        /// </summary>
        /// <param name="errorMsg"></param>
        /// <returns></returns>
        public static ApiResult<T> Error<T>(string errorMsg)
        {
            return new ApiResult<T>
            {
                Code = Status.Error,
                Message = errorMsg
            };
        }

        /// <summary>
        /// 设置失败状态
        /// </summary>
        /// <param name="failMsg"></param>
        /// <returns></returns>
        public static ApiResult<T> Fail<T>(string failMsg)
        {
            return new ApiResult<T>
            {
                Code = Status.Fail,
                Message = failMsg
            };
        }
    }
}