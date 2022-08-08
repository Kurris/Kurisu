using System;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 服务注册,不继承
    /// </summary>
    [SkipScan]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class RegisterAttribute : Attribute
    {
        /// <summary>
        /// 服务注册
        /// </summary>
        /// <param name="name">命名命名</param>
        public RegisterAttribute(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// 服务注册
        /// </summary>
        /// <param name="interceptors">代理器</param>
        public RegisterAttribute(params Type[] interceptors)
        {
            this.Interceptors = interceptors;
        }


        /// <summary>
        /// 服务注册
        /// </summary>
        /// <param name="name">命名命名</param>
        /// <param name="interceptors">代理器</param>
        public RegisterAttribute(string name, params Type[] interceptors)
        {
            this.Name = name;
            this.Interceptors = interceptors;
        }

        /// <summary>
        /// 服务命名
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 代理器
        /// </summary>
        public Type[] Interceptors { get; }
    }
}