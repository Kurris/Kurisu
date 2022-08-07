using System;

namespace Kurisu.MVC
{
    /// <summary>
    /// 接口定义
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ApiDefinitionAttribute : Attribute
    {
        public ApiDefinitionAttribute(string title)
        {
            Title = title;
        }

        public string Title { get; }
    }
}