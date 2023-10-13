namespace Kurisu.Utils.Extensions;

/// <summary>
/// 字符串扩展类
/// </summary>
public static class StringExtensions
{
    public static bool IsEmpty(this string str)
    {
        return string.IsNullOrEmpty(str);
    }

    public static bool IsNotEmpty(this string str)
    {
        return !str.IsEmpty();
    }

    public static bool IsNullOrWhiteSpace(this string str)
    {
        return string.IsNullOrWhiteSpace(str);
    }
}