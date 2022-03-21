namespace Kurisu.Common.Dto
{
    /// <summary>
    /// 分页入参数
    /// </summary>
    public class PageIn
    {
        public int PageIndex { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }
}