namespace Kurisu.AspNetCore.Utils.Extensions;

/// <summary>
/// 字符串扩展类
/// </summary>
public static class StringExtensions
{
    public static bool IsEmpty(this string str)
    {
        return string.IsNullOrEmpty(str);
    }

    public static bool IsPresent(this string str)
    {
        return !str.IsEmpty();
    }

    public static bool IsWhiteSpace(this string str)
    {
        return string.IsNullOrWhiteSpace(str);
    }
}