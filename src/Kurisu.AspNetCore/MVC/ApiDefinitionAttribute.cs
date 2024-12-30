using System;

namespace Kurisu.AspNetCore.MVC;

/// <summary>
/// 接口定义
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ApiDefinitionAttribute : Attribute
{
    /// <summary>
    /// 定义api标题组
    /// </summary>
    /// <param name="group"></param>
    public ApiDefinitionAttribute(string group)
    {
        Group = group;
    }

    /// <summary>
    /// api分组
    /// </summary>
    public string Group { get; }

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; }
}