using System;
using Kurisu.DependencyInjection.Enums;

namespace Kurisu.DependencyInjection.Attributes
{
    /// <summary>
    /// 依赖注入特性
    /// </summary>
    [SkipScan]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class RegisterAttribute : Attribute
    {
        public RegisterAttribute()
        {
        }

        /// <summary>
        /// 注册类型,默认瞬时
        /// </summary>
        public RegisterType RegisterType { get; set; } = RegisterType.Transient;

        /// <summary>
        /// 服务命名
        /// </summary>
        public string Named { get; set; }

        /// <summary>
        /// 代理类
        /// </summary>
        public Type Proxy { get; set; }
    }
}