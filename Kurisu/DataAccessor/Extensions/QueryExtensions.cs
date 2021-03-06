using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Abstractions;
using Kurisu.DataAccessor.Dto;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccessor.Extensions
{
    /// <summary>
    /// 查询扩展类
    /// </summary>
    public static class QueryExtensions
    {
        /// <summary>
        /// 获取分页数据
        /// </summary>
        /// <param name="entities">可查询数据</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页数</param>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <returns></returns>
        public static Pagination<TEntity> ToPage<TEntity>(this IQueryable<TEntity> entities
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
        /// <param name="queryable">可查询数据</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">实体类型</param>
        /// <param name="cancellationToken">终止信号令牌</param>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public static async Task<Pagination<TEntity>> ToPageAsync<TEntity>(this IQueryable<TEntity> queryable
            , int pageIndex = 1
            , int pageSize = 20
            , CancellationToken cancellationToken = default)
            where TEntity : class, new()
        {
            if (pageIndex <= 0) throw new ArgumentOutOfRangeException(nameof(pageIndex));
            if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));

            var totalCount = await queryable.CountAsync(cancellationToken);
            var items = await queryable.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

            return new Pagination<TEntity>
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                Total = totalCount,
                Data = items
            };
        }

        /// <summary>
        /// 获取分页数据
        /// </summary>
        /// <param name="queryable"></param>
        /// <param name="input"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public static async Task<Pagination<TEntity>> ToPageAsync<TEntity>(this IQueryable<TEntity> queryable, PageInput input) where TEntity : class, new()
        {
            return await queryable.ToPageAsync(input.PageIndex, input.PageSize);
        }


        /// <summary>
        /// Where查询
        /// </summary>
        /// <param name="queryable"></param>
        /// <param name="whenTodo"></param>
        /// <param name="predicate"></param>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public static IQueryable<TEntity> WhereIf<TEntity>(this IQueryable<TEntity> queryable, bool whenTodo, Expression<Func<TEntity, bool>> predicate)
        {
            return whenTodo ? queryable.Where(predicate) : queryable;
        }


        /// <summary>
        /// 动态排序
        /// </summary>
        /// <param name="queryable"></param>
        /// <param name="sort"></param>
        /// <param name="isAsc"></param>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public static IQueryable<TEntity> Sort<TEntity>(this IQueryable<TEntity> queryable, string sort, bool isAsc = true) where TEntity : class, new()
        {
            var sortArr = sort.Split(',');

            for (var i = 0; i < sortArr.Length; i++)
            {
                var sortColAndRuleArr = sortArr[i].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var sortField = sortColAndRuleArr.First();
                var sortAsc = isAsc;

                //排序列带上规则   "Id Asc"
                if (sortColAndRuleArr.Length == 2)
                {
                    sortAsc = string.Equals(sortColAndRuleArr[1], "asc", StringComparison.OrdinalIgnoreCase);
                }

                var parameter = Expression.Parameter(typeof(TEntity), "type");
                var property = typeof(TEntity).GetProperties().First(p => p.Name.Equals(sortField));
                var propertyAccess = Expression.MakeMemberAccess(parameter, property);
                var orderByExpression = Expression.Lambda(propertyAccess, parameter);

                MethodCallExpression resultExpression;
                if (i == 0)
                {
                    resultExpression = Expression.Call(
                        typeof(Queryable), //调用的类型
                        sortAsc ? "OrderBy" : "OrderByDescending", //方法名称
                        new[] { typeof(TEntity), property.PropertyType }, queryable.Expression, Expression.Quote(orderByExpression));
                }
                else
                {
                    resultExpression = Expression.Call(
                        typeof(Queryable),
                        sortAsc ? "ThenBy" : "ThenByDescending",
                        new[] { typeof(TEntity), property.PropertyType }, queryable.Expression, Expression.Quote(orderByExpression));
                }

                queryable = queryable.Provider.CreateQuery<TEntity>(resultExpression);
            }

            return queryable;
        }


        /// <summary>
        /// select映射dto, 使用mapster ProjectToType
        /// </summary>
        /// <typeparam name="TDto"></typeparam>
        /// <param name="queryable"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static IQueryable<TDto> Select<TDto>(this IQueryable queryable, TypeAdapterConfig config = null)
        {
            return queryable.ProjectToType<TDto>(config);
        }

        public static IQueryable<TEntity> Where<TEntity>(this IAppDbService appDbService, Expression<Func<TEntity, bool>> predicate, bool userMasterDb = false)
            where TEntity : class, new()
        {
            return appDbService.Queryable<TEntity>(userMasterDb).Where(predicate);
        }

        public static IQueryable<TEntity> Where<TEntity>(this IAppSlaveDb appDbService, Expression<Func<TEntity, bool>> predicate)
           where TEntity : class, new()
        {
            return appDbService.Queryable<TEntity>().Where(predicate);
        }
    }
}