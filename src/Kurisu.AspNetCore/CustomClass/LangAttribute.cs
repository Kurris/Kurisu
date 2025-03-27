using System;

namespace Kurisu.AspNetCore.CustomClass;

/// <summary>
/// 多语言描述
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
public class LangAttribute : Attribute
{
    /// <summary>
    /// 语言描述
    /// </summary>
    /// <param name="display"></param>
    public LangAttribute(string display) : this(display, "cn")
    {
    }

    /// <summary>
    /// 语言描述
    /// </summary>
    /// <param name="display"></param>
    /// <param name="lang"></param>
    public LangAttribute(string display, string lang)
    {
        Lang = lang;
        Display = display;
    }

    /// <summary>
    /// 描述
    /// </summary>
    public string Display { get; }

    /// <summary>
    /// 语言
    /// </summary>
    public string Lang { get; }
}