namespace Kurisu.Utils.Extensions
{
    /// <summary>
    /// 字符串工具类
    /// </summary>
    public static class StringUtils
    {
        /// <summary>
        /// 是否为空
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsEmpty(this string str)
        {
            return string.IsNullOrEmpty(str?.Trim());
        }
    }
}