using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccessor.UnitOfWork.Abstractions
{
    /// <summary>
    /// UnitOfWork
    /// </summary>
    public interface IUnitOfWorkDbContext
    {
        /// <summary>
        /// 是否自动提交
        /// </summary>
        bool IsAutomaticSaveChanges { get; set; }

        /// <summary>
        /// 获取实现工作单元的DbContext
        /// </summary>
        /// <returns></returns>
        DbContext GetUnitOfWorkDbContext();
    }
}