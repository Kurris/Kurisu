using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccessor.Functions.UnitOfWork.Abstractions;

/// <summary>
/// UnitOfWork
/// </summary>
public interface IUnitOfWorkDbContext
{
    /// <summary>
    /// 是否自动提交
    /// <remarks>
    /// 当命令执行后,调用SaveChanges提交
    /// </remarks>
    /// </summary>
    bool IsAutomaticSaveChanges { get; set; }

    /// <summary>
    /// 获取实现工作单元的DbContext
    /// </summary>
    /// <returns></returns>
    DbContext GetUnitOfWorkDbContext();
}