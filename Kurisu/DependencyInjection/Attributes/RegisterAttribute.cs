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
        public RegisterAttribute()
        {
        }

        public RegisterAttribute(string name)
        {
            this.Name = name;
        }

        public RegisterAttribute(params Type[] interceptors)
        {
            this.Interceptors = interceptors;
        }


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
        /// 拦截器
        /// </summary>
        public Type[] Interceptors { get; }
    }
}