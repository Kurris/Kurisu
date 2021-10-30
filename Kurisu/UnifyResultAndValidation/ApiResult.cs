using System;

namespace Kurisu.UnifyResultAndValidation
{
    /// <summary>
    /// 数据结果返回模型
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public class ApiResult<T>
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
            this.Status = Status.Error;
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
        /// 状态(默认:Error 5000)
        /// </summary>
        public Status Status { get; set; }
    }

    /// <summary>
    /// 返回状态
    /// </summary>
    public enum Status
    {
        /// <summary>
        /// 登录成功
        /// </summary>
        LoginSuccess = 1000,

        /// <summary>
        /// 操作成功
        /// </summary>
        Success = 1001,


        /// <summary>
        /// 操作失败
        /// </summary>
        Fail = 1002,

        /// <summary>
        /// 操作被取消
        /// </summary>
        Cancel = 1003,


        /// <summary>
        /// 鉴权失败
        /// </summary>
        AuthorizationFail = 4000,

        /// <summary>
        /// 无权限
        /// </summary>
        NoPermission = 4001,


        /// <summary>
        /// 实体验证失败
        /// </summary>
        ValidateEntityError = 4002,

        /// <summary>
        /// 执行异常
        /// </summary>
        Error = 5000,
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
                Status = Status.Success,
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
                Status = Status.Error,
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
                Status = Status.Error,
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
                Status = Status.Fail,
                Message = failMsg
            };
        }

        /// <summary>
        /// 设置取消状态
        /// </summary>
        /// <param name="cancelMsg"></param>
        /// <returns></returns>
        public static ApiResult<T> Cancel<T>(string cancelMsg)
        {
            return new ApiResult<T>
            {
                Status = Status.Cancel,
                Message = cancelMsg
            };
        }
    }
}