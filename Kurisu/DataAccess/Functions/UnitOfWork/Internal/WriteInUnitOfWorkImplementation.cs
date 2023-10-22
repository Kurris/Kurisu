//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Kurisu.DataAccess.Functions.Default.Internal;
//using Kurisu.DataAccess.Functions.UnitOfWork.Abstractions;
//using Microsoft.EntityFrameworkCore;

//namespace Kurisu.DataAccess.Functions.UnitOfWork.Internal;

///// <summary>
///// 工作单元,写实现
///// </summary>
//internal class WriteInUnitOfWorkImplementation : WriteImplementation
//{
//    private readonly IUnitOfWorkDbContext _unitOfWorkDbContext;

//    public WriteInUnitOfWorkImplementation(DbContext dbContext) : base(dbContext)
//    {
//        if (!dbContext.GetType().IsAssignableTo(typeof(IUnitOfWorkDbContext)))
//        {
//            throw new ArgumentException(nameof(dbContext) + " 尚未实现IUnitOfWorkDbContext");
//        }

//        _unitOfWorkDbContext = (IUnitOfWorkDbContext) dbContext;
//    }


//    public override async Task DeleteByIdsAsync<T>(params object[] keyValues)
//    {
//        await base.DeleteByIdsAsync<T>(keyValues);
//        await SaveChangesToDatabaseAsync();
//    }


//    public override async Task DeleteAsync<T>(T entity)
//    {
//        await base.DeleteAsync(entity);
//        await SaveChangesToDatabaseAsync();
//    }

//    public override async Task DeleteAsync(object entity)
//    {
//        await base.DeleteAsync(entity);
//        await SaveChangesToDatabaseAsync();
//    }


//    public override async Task UpdateAsync(object entity, bool updateAll = false)
//    {
//        await base.UpdateAsync(entity, updateAll);
//        await SaveChangesToDatabaseAsync();
//    }

//    public override async Task UpdateAsync<T>(T entity, bool updateAll = false)
//    {
//        await base.UpdateAsync(entity, updateAll);
//        await SaveChangesToDatabaseAsync();
//    }

//    public override async Task UpdateRangeAsync<T>(IEnumerable<T> entities, bool updateAll = false)
//    {
//        await base.UpdateRangeAsync(entities, updateAll);
//        await SaveChangesToDatabaseAsync();
//    }

//    public override async Task DeleteRangeAsync<T>(IEnumerable<T> entities)
//    {
//        await DeleteRangeAsync((IEnumerable<object>) entities);
//        await SaveChangesToDatabaseAsync();
//    }

//    public override async Task DeleteRangeAsync(IEnumerable<object> entities)
//    {
//        await base.DeleteRangeAsync(entities);
//        await SaveChangesToDatabaseAsync();
//    }

//    public override async ValueTask SaveRangeAsync(IEnumerable<object> entities)
//    {
//        await base.SaveAsync(entities);
//        await SaveChangesToDatabaseAsync();
//    }

//    public override async ValueTask SaveAsync(object entity)
//    {
//        await base.SaveAsync(entity);
//        await SaveChangesToDatabaseAsync();
//    }

//    public override async ValueTask SaveRangeAsync<T>(IEnumerable<T> entities)
//    {
//        await base.SaveRangeAsync(entities);
//        await SaveChangesToDatabaseAsync();
//    }

//    public override async ValueTask SaveAsync<T>(T entity)
//    {
//        await base.SaveAsync(entity);
//        await SaveChangesToDatabaseAsync();
//    }

//    /// <summary>
//    /// 提交写入数据库
//    /// </summary>
//    /// <returns></returns>
//    private async Task<int> SaveChangesToDatabaseAsync()
//    {
//        // 自动提交
//        if (_unitOfWorkDbContext.IsAutomaticSaveChanges)
//            return 0;

//        return await base.SaveChangesAsync();
//    }
//}