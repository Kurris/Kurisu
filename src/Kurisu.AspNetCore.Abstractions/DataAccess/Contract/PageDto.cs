namespace Kurisu.AspNetCore.Abstractions.DataAccess.Contract;

/// <summary>
/// 分页Dto
/// </summary>
public class PageDto
{
    /// <summary>
    /// ctor
    /// </summary>
    public PageDto()
    {
        PageIndex = 1;
        PageSize = 20;
    }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="pageIndex"></param>
    /// <param name="pageSize"></param>
    public PageDto(int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
    }

    /// <summary>
    /// index
    /// </summary>
    public int PageIndex { get; set; }


    /// <summary>
    /// size
    /// </summary>
    public int PageSize { get; set; }


    /// <summary>
    /// 模糊搜索
    /// </summary>
    public string Search { get; set; }

    /// <summary>
    /// 是否导出
    /// </summary>
    public bool IsExport { get; set; }
}