using System;

namespace Kurisu.AspNetCore.CustomClass;

/// <summary>
/// 英文描述
/// </summary>
[AttributeUsage(AttributeTargets.All, Inherited = false)]
public class DescriptionEnAttribute : Attribute
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="description"></param>
    public DescriptionEnAttribute(string description)
    {
        Description = description;
    }

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; }
}