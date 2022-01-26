using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Kurisu.DataAccessor
{
    /// <summary>
    /// 分页参数
    /// </summary>
    /// <typeparam name="TEntity">数据实体</typeparam>
    public class Pagination<TEntity> where TEntity : class, new()
    {
        /// <summary>
        /// 页码
        /// </summary>
        public int PageIndex { get; set; }

        /// <summary>
        /// 页容量
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// 总条数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages
        {
            get
            {
                if (TotalCount > 0)
                {
                    return TotalCount % PageSize == 0
                        ? TotalCount / PageSize
                        : TotalCount / PageSize + 1;
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
        public IEnumerable<TEntity> Items { get; set; }
    }

    /// <summary>
    /// 分页参数
    /// </summary>
    public class Pagination : Pagination<object>
    {
    }
}