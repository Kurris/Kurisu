using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Functions.Default.Internal;
using Kurisu.DataAccessor.Functions.UnitOfWork.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccessor.Functions.UnitOfWork.Internal
{
    /// <summary>
    /// 工作单元,写实现
    /// </summary>
    public class WriteInUnitOfWorkImplementation : WriteImplementation
    {
        public WriteInUnitOfWorkImplementation(DbContext dbContext) : base(dbContext)
        {
            if (!dbContext.GetType().IsAssignableTo(typeof(IUnitOfWorkDbContext)))
            {
                throw new ArgumentException(nameof(dbContext) + " 尚未实现IUnitOfWorkDbContext");
            }
        }

        public override async Task DeleteByIdAsync<T>(object keyValue)
        {
            await base.DeleteByIdAsync<T>(keyValue);
            await CommitToDatabaseAsync();
        }


        public override async Task DeleteAsync<T>(T entity)
        {
            await base.DeleteAsync(entity);
            await CommitToDatabaseAsync();
        }

        public override async Task DeleteAsync(object entity)
        {
            await base.DeleteAsync(entity);
            await CommitToDatabaseAsync();
        }

        public override async ValueTask InsertAsync(object entity)
        {
            await base.InsertAsync(entity);
            await CommitToDatabaseAsync();
        }

        public override async ValueTask InsertAsync<T>(T entity)
        {
            await base.InsertAsync(entity);
            await CommitToDatabaseAsync();
        }


        public override async Task InsertRangeAsync<T>(IEnumerable<T> entities)
        {
            await base.InsertRangeAsync(entities);
            await CommitToDatabaseAsync();
        }

        public override async Task InsertRangeAsync(IEnumerable<object> entities)
        {
            await base.InsertRangeAsync(entities);
            await CommitToDatabaseAsync();
        }

        public override async Task UpdateAsync(object entity, bool updateAll = false)
        {
            await base.UpdateAsync(entity, updateAll);
            await CommitToDatabaseAsync();
        }

        public override async Task UpdateAsync<T>(T entity, bool updateAll = false)
        {
            await base.UpdateAsync(entity, updateAll);
            await CommitToDatabaseAsync();
        }

        public override async Task UpdateRangeAsync<T>(IEnumerable<T> entities, bool updateAll = false)
        {
            await base.UpdateRangeAsync(entities, updateAll);
            await CommitToDatabaseAsync();
        }

        public override async Task DeleteRangeAsync<T>(IEnumerable<T> entities)
        {
            await base.DeleteRangeAsync(entities);
            await CommitToDatabaseAsync();
        }

        public override async Task DeleteRangeAsync(IEnumerable<object> entities)
        {
            await base.DeleteRangeAsync(entities);
            await CommitToDatabaseAsync();
        }

        public override async ValueTask SaveAsync(IEnumerable<object> entities)
        {
            await base.SaveAsync(entities);
            await CommitToDatabaseAsync();
        }

        public override async ValueTask SaveAsync(object entity)
        {
            await base.SaveAsync(entity);
            await CommitToDatabaseAsync();
        }

        public override async ValueTask SaveAsync<T>(IEnumerable<T> entities)
        {
            await base.SaveAsync(entities);
            await CommitToDatabaseAsync();
        }

        public override async ValueTask SaveAsync<T>(T entity)
        {
            await base.SaveAsync(entity);
            await CommitToDatabaseAsync();
        }

        /// <summary>
        /// 提交写入数据库
        /// </summary>
        /// <returns></returns>
        private async Task<int> CommitToDatabaseAsync()
        {
            //是否unitofwork
            if (DbContext.GetType().IsAssignableTo(typeof(IUnitOfWorkDbContext)))
            {
                // 自动提交
                if (((IUnitOfWorkDbContext) DbContext).IsAutomaticSaveChanges)
                {
                    return 0;
                }
            }

            return await base.SaveChangesAsync();
        }
    }
}