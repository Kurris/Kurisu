using System;

namespace Kurisu.MVC
{
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