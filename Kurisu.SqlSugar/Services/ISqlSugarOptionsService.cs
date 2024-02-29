namespace Kurisu.SqlSugar.Services;

/// <summary>
/// sugar配置服务
/// </summary>
public interface ISqlSugarOptionsService
{
    /// <summary>
    /// 日志标题
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 路由地址
    /// </summary>
    public string RoutePath { get; set; }

    /// <summary>
    /// 是否记录差异
    /// </summary>
    public bool Diff { get; set; }

    /// <summary>
    /// 批次号
    /// </summary>
    public Guid BatchNo { get; set; }

    /// <summary>
    /// 触发时间
    /// </summary>
    public DateTime RaiseTime { get; set; }

    public bool IgnoreTenant { get; set; }
}