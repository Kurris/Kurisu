using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kurisu.Common.Dto;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccessor.Extensions
{
    /// <summary>
    /// 分页查询
    /// </summary>
    public static class PaginationQueryExtensions
    {
        /// <summary>
        /// 获取分页数据
        /// </summary>
        /// <param name="entities">可查询数据</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页数</param>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <returns></returns>
        public static Pagination<TEntity> ToPagedList<TEntity>(this IQueryable<TEntity> entities
            , int pageIndex = 1
            , int pageSize = 20)
            where TEntity : class, new()
        {
            var total = entities.Count();
            var items = entities.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();

            return new Pagination<TEntity>
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                Total = total,
                Data = items
            };
        }

        /// <summary>
        /// 获取分页数据
        /// </summary>
        /// <param name="entities">可查询数据</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">实体类型</param>
        /// <param name="cancellationToken">终止信号令牌</param>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public static async Task<Pagination<TEntity>> ToPagedListAsync<TEntity>(this IQueryable<TEntity> entities
            , int pageIndex = 1
            , int pageSize = 20
            , CancellationToken cancellationToken = default)
            where TEntity : class, new()
        {
            if (pageIndex <= 0) throw new ArgumentOutOfRangeException(nameof(pageIndex));
            if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));

            var totalCount = await entities.CountAsync(cancellationToken);
            var items = await entities.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

            return new Pagination<TEntity>
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                Total = totalCount,
                Data = items
            };
        }
    }
}