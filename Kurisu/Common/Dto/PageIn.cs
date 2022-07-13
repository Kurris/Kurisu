namespace Kurisu.Common.Dto
{
    /// <summary>
    /// 分页入参数
    /// </summary>
    public class PageInput
    {
        /// <summary>
        /// 当前页
        /// </summary>
        public int PageIndex { get; set; } = 1;


        /// <summary>
        /// 页码
        /// </summary>
        public int PageSize { get; set; } = 20;
    }
}