namespace Kurisu.Core.DataAccess.Dto;

public class PageDto
{
    public PageDto()
    {
        PageIndex = 1;
        PageSize = 20;
    }

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
}