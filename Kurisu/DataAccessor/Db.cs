using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Kurisu.DataAccessor
{
    public static class Db
    {
        /// <summary>
        /// 动态排序
        /// </summary>
        /// <param name="tempData"></param>
        /// <param name="sort"></param>
        /// <param name="isAsc"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        internal static IQueryable<T> Sort<T>(IQueryable<T> tempData, string sort, bool isAsc) where T : class
        {
            var sortArr = sort.Split(',');
            MethodCallExpression resultExpression = null;

            for (var i = 0; i < sortArr.Length; i++)
            {
                var sortColAndRuleArr = sortArr[i].Trim().Split(new string[] {" "}, StringSplitOptions.RemoveEmptyEntries);
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

                if (i == 0)
                {
                    resultExpression = Expression.Call(
                        typeof(Queryable), //调用的类型
                        sortAsc ? "OrderBy" : "OrderByDescending", //方法名称
                        new[] {typeof(T), property.PropertyType}, tempData.Expression, Expression.Quote(orderByExpression));
                }
                else
                {
                    resultExpression = Expression.Call(
                        typeof(Queryable),
                        sortAsc ? "ThenBy" : "ThenByDescending",
                        new[] {typeof(T), property.PropertyType}, tempData.Expression, Expression.Quote(orderByExpression));
                }

                tempData = tempData.Provider.CreateQuery<T>(resultExpression);
            }

            return tempData;
        }

        /// <summary>
        /// 递归附加
        /// </summary>
        /// <param name="dbContext">当前上下文</param>
        /// <param name="entity">实例</param>
        internal static void RecursionAttach(DbContext dbContext, object entity)
        {
            var entityType = FindTrackingEntity(dbContext, entity);

            if (entityType == null)
                dbContext.Attach(entity);
            else if (entityType.State == EntityState.Modified || entityType.State == EntityState.Added)
                return;

            foreach (var prop in entity.GetType().GetProperties().Where(x => !x.IsDefined(typeof(NotMappedAttribute), false)))
            {
                if (prop.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)) continue;

                var obj = prop.GetValue(entity);
                if (obj == null) continue;

                var subEntityType = FindTrackingEntity(dbContext, obj);

                //List<Entity>
                if (prop.PropertyType.IsGenericType && prop.PropertyType.IsClass)
                {
                    IEnumerable<object> objs = (IEnumerable<object>) obj;
                    foreach (var item in objs)
                    {
                        RecursionAttach(dbContext, item);
                    }
                }
                //string/int
                else if (subEntityType == null)
                {
                    dbContext.Entry(entity).Property(prop.Name).IsModified = true;
                }
                //Entity
                else if (subEntityType != null && subEntityType.State == EntityState.Unchanged)
                {
                    RecursionAttach(dbContext, obj);
                }
            }
        }

        /// <summary>
        /// 根据ID匹配是否存在
        /// </summary>
        /// <param name="dbContext">当前上下文</param>
        /// <param name="entity">实例</param>
        /// <returns></returns>
        private static EntityEntry FindTrackingEntity(DbContext dbContext, object entity)
        {
            foreach (var item in dbContext.ChangeTracker.Entries())
            {
                if (item.State == EntityState.Added)
                {
                    if (item.Entity == entity)
                    {
                        return item;
                    }
                }

                var key = "Id";

                var tryObj = item.Properties.FirstOrDefault(x => x.Metadata.PropertyInfo.Name.Equals(key))?.CurrentValue;
                if (tryObj != null)
                {
                    int tracking = (int) tryObj;
                    int now = (int) entity.GetType().GetProperty(key)?.GetValue(entity);

                    if (tracking == now)
                    {
                        return item;
                    }
                }
            }

            return null;
        }
    }
}