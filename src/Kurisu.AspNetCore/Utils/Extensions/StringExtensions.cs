using System.Diagnostics.CodeAnalysis;

namespace Kurisu.AspNetCore.Utils.Extensions;

/// <summary>
/// 字符串扩展类
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// 不存在值
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static bool IsEmpty(this string str)
    {
        return string.IsNullOrEmpty(str);
    }

    /// <summary>
    /// 存在值
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static bool IsPresent([NotNullWhen(true)] this string str)
    {
        return !str.IsEmpty();
    }

    /// <summary>
    /// null或者有空白
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static bool IsWhiteSpace(this string str)
    {
        return string.IsNullOrWhiteSpace(str);
    }
}