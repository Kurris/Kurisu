namespace Kurisu.DataAccessor
{
    /// <summary>
    /// 分页入参数
    /// </summary>
    public class PageIn
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public bool Desc { get; set; } = true;
    }
}