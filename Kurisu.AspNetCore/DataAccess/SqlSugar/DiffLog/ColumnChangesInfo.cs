namespace Kurisu.AspNetCore.DataAccess.SqlSugar.DiffLog;

/// <summary>
/// 列变换数据
/// </summary>
public class ColumnChangesDetail
{
    /// <summary>
    /// 列
    /// </summary>
    public string Column { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 明细
    /// </summary>
    public string Detail { get; set; }
}
