using System;

namespace Kurisu.AspNetCore.CustomClass;

/// <summary>
/// 用于为目标添加多语言描述的特性。
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
public class LangAttribute : Attribute
{
    /// <summary>
    /// 初始化 <see cref="LangAttribute"/> 类的新实例。
    /// </summary>
    /// <param name="display">描述文本。</param>
    /// <param name="lang">语言代码，默认为 "cn"。</param>
    public LangAttribute(string display, string lang = "cn")
    {
        Lang = lang;
        Description = display;
    }

    /// <summary>
    /// 获取描述文本。
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// 获取语言代码。
    /// </summary>
    public string Lang { get; }
}