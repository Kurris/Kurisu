using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccessor.Abstractions
{
    /// <summary>
    /// 数据库连接容器
    /// </summary>
    public interface IDbContextContainer
    {
        /// <summary>
        /// DbContext数据
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// 是否自动提交
        /// </summary>
        public bool IsAutomaticSaveChanges { get;  set; }

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
        /// <returns></returns>
        Task<int> SaveChangesAsync();

        /// <summary>
        /// 保存所有数据库上下文的更改
        /// </summary>
        /// <param name="acceptAllChangesOnSuccess"></param>
        /// <returns></returns>
        Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess);

        /// <summary>
        /// 打开事务
        /// </summary>
        /// <returns></returns>
        Task<IDbContextContainer> BeginTransactionAsync();

        /// <summary>
        /// 提交事务
        /// </summary>
        /// <param name="exception"></param>
        Task CommitTransactionAsync(Exception exception = null);
    }
}