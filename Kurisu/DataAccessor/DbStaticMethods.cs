using System;
using System.Linq;
using System.Linq.Expressions;

namespace Kurisu.DataAccessor
{
    public static class DbStaticMethods
    {
        /// <summary>
        /// 动态排序
        /// </summary>
        /// <param name="queryable"></param>
        /// <param name="sort"></param>
        /// <param name="isAsc"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IQueryable<T> Sort<T>(this IQueryable<T> queryable, string sort, bool isAsc) where T : class
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

                var parameter = Expression.Parameter(typeof(T), "type");
                var property = typeof(T).GetProperties().First(p => p.Name.Equals(sortField));
                var propertyAccess = Expression.MakeMemberAccess(parameter, property);
                var orderByExpression = Expression.Lambda(propertyAccess, parameter);

                MethodCallExpression resultExpression;
                if (i == 0)
                {
                    resultExpression = Expression.Call(
                        typeof(Queryable), //调用的类型
                        sortAsc ? "OrderBy" : "OrderByDescending", //方法名称
                        new[] {typeof(T), property.PropertyType}, queryable.Expression, Expression.Quote(orderByExpression));
                }
                else
                {
                    resultExpression = Expression.Call(
                        typeof(Queryable),
                        sortAsc ? "ThenBy" : "ThenByDescending",
                        new[] {typeof(T), property.PropertyType}, queryable.Expression, Expression.Quote(orderByExpression));
                }

                queryable = queryable.Provider.CreateQuery<T>(resultExpression);
            }

            return queryable;
        }
    }
}