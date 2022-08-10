using System;

namespace Kurisu.MVC
{
    /// <summary>
    /// 接口定义
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ApiDefinitionAttribute : Attribute
    {
        /// <summary>
        /// 定义api标题组
        /// </summary>
        /// <param name="title"></param>
        public ApiDefinitionAttribute(string title)
        {
            Title = title;
        }

        /// <summary>
        /// api 标题组
        /// </summary>
        public string Title { get; }
    }
}