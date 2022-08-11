using System;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.UnifyResultAndValidation
{
    /// <summary>
    /// 用户友好异常
    /// </summary>
    [SkipScan]
    public class UserFriendlyException : Exception
    {
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="errorMessage">错误信息</param>
        public UserFriendlyException(string errorMessage) : base(errorMessage)
        {
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="exception">异常</param>
        public UserFriendlyException(Exception exception) : base(exception.Message)
        {
        }
    }
}