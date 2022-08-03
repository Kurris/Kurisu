using System.Collections.Generic;

namespace Kurisu.DataAccessor.Dto
{
    /// <summary>
    /// 分页参数
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public class Pagination<T>
    {
        /// <summary>
        /// 页码
        /// </summary>
        public int PageIndex { get; internal set; }

        /// <summary>
        /// 页容量
        /// </summary>
        public int PageSize { get; internal set; }

        /// <summary>
        /// 总条数
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages
        {
            get
            {
                if (Total > 0)
                {
                    return Total % PageSize == 0
                        ? Total / PageSize
                        : Total / PageSize + 1;
                }

                return 0;
            }
        }

        /// <summary>
        /// 是否有上一页
        /// </summary>
        public bool HasPrevPages => PageIndex - 1 > 0;

        /// <summary>
        /// 是否有下一页
        /// </summary>
        public bool HasNextPages => PageIndex < TotalPages;

        /// <summary>
        /// 当前页集合
        /// </summary>
        public List<T> Data { get; set; }
    }

    /// <summary>
    /// 分页参数
    /// </summary>
    public class Pagination : Pagination<object>
    {
    }
}