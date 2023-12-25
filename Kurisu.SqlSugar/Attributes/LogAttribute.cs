using Kurisu.Core.Proxy.Attributes;
using Kurisu.SqlSugar.Aops;

namespace Kurisu.SqlSugar.Attributes;

/// <summary>
/// 日志记录
/// </summary>
public class LogAttribute : AopAttribute
{
    public LogAttribute(string title)
    {
        Title = title;
        if (Interceptors?.Any() != true)
        {
            Interceptors = new[] { typeof(Log) };
        }
    }

    public string Title { get; }

    /// <summary>
    /// 记录差异
    /// </summary>
    public bool Diff { get; set; }
}