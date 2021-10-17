using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kurisu.DataAccessor.Abstractions
{
    /// <summary>
    /// 数据库连接容器
    /// </summary>
    public interface IDbContextContainer
    {
        /// <summary>
        /// 数据库上下文事务
        /// </summary>
        public IDbContextTransaction DbContextTransaction { get; set; }

        /// <summary>
        /// 获取所有数据库上下文
        /// </summary>
        /// <returns></returns>
        ConcurrentDictionary<Guid, DbContext> GetDbContexts();

        /// <summary>
        /// 添加上下文到容器中
        /// </summary>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        void Add(DbContext dbContext);


        /// <summary>
        /// 保存所有数据库上下文的更改
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 保存所有数据库上下文的更改
        /// </summary>
        /// <param name="acceptAllChangesOnSuccess"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default);


        /// <summary>
        /// 打开事务
        /// </summary>
        /// <param name="ensureTransaction"></param>
        /// <returns></returns>
        Task<IDbContextContainer> BeginTransactionAsync(bool ensureTransaction = false);

        /// <summary>
        /// 提交事务
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="isManualSaveChanges"></param>
        Task CommitTransactionAsync(bool isManualSaveChanges = true, Exception exception = default);


        /// <summary>`
        /// 关闭所有数据库连接
        /// </summary>
        Task CloseAsync();
    }
}