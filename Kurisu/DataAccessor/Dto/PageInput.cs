using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.DataAccessor.Dto
{
    /// <summary>
    /// 分页入参数
    /// </summary>
    [SkipScan]
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