using Kurisu.UnifyResultAndValidation.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.UnifyResultAndValidation
{
    /// <summary>
    /// 数据结果返回模型
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    [SkipScan]
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
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
            this.Status = Status.Error;
        }

        /// <summary>
        /// 数据结果返回模型
        /// </summary>
        /// <param name="message">信息</param>
        /// <param name="data">数据</param>
        /// <param name="status">状态</param>
        public DefaultApiResult(string message, T data, Status status)
        {
            this.Message = message;
            this.Data = data;
            this.Status = status;
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
        public Status Status { get; set; }


        public virtual IApiResult GetDefaultSuccessApiResult<TResult>(TResult apiResult)
        {
            return new DefaultApiResult<TResult>
            {
                Status = Status.Success,
                Message = "操作成功",
                Data = apiResult
            };
        }

        public virtual IApiResult GetDefaultValidateApiResult<TResult>(TResult apiResult)
        {
            return new DefaultApiResult<TResult>
            {
                Status = Status.ValidateError,
                Message = "实体验证失败",
                Data = apiResult
            };
        }

        public virtual IApiResult GetDefaultForbiddenApiResult()
        {
            return new DefaultApiResult<object>
            {
                Status = Status.Forbidden,
                Message = "无权访问"
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
    public enum Status
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
}