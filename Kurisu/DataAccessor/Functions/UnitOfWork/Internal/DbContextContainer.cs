using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Functions.UnitOfWork.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.DataAccessor.Functions.UnitOfWork.Internal
{
    /// <summary>
    /// 数据库上下文容器
    /// </summary>
    [SkipScan]
    public sealed class DbContextContainer : IDbContextContainer
    {
        /// <summary>
        /// 有效上下文
        /// </summary>
        private readonly ConcurrentDictionary<Guid, DbContext> _dbContexts;

        /// <summary>
        /// 失败上下文
        /// </summary>
        private static readonly ConcurrentDictionary<Guid, DbContext> FailedDbContexts = new();

        /// <summary>
        /// 是否自动提交
        /// </summary>
        private bool _isAutomaticSaveChanges;

        /// <summary>
        /// 是否在运行中
        /// </summary>
        public bool IsRunning { get; private set; }


        /// <summary>
        /// ctor
        /// </summary>
        public DbContextContainer()
        {
            _dbContexts = new ConcurrentDictionary<Guid, DbContext>();
        }


        /// <summary>
        /// 数据库上下文个数
        /// </summary>
        public int Count => _dbContexts.Count;

        /// <summary>
        /// 是否自动提交
        /// </summary>
        public bool IsAutomaticSaveChanges
        {
            get => _isAutomaticSaveChanges;
            set
            {
                _isAutomaticSaveChanges = value;
                var dbContexts = _dbContexts.Select(x => x.Value);
                foreach (var dbContext in dbContexts)
                {
                    // ReSharper disable once SuspiciousTypeConversion.Global
                    ((IUnitOfWorkDbContext) dbContext).IsAutomaticSaveChanges = value;
                }
            }
        }

        /// <summary>
        /// 数据库上下文事务
        /// </summary>
        private IDbContextTransaction DbContextTransaction { get; set; }


        /// <summary>
        /// 添加上下文到容器中
        /// </summary>
        /// <param name="dbContext"></param>
        public void Manage(DbContext dbContext)
        {
            //非关系型数据库
            if (!dbContext.Database.IsRelational())
                return;

            //排除只读
            if (dbContext.ChangeTracker.QueryTrackingBehavior is QueryTrackingBehavior.NoTracking or QueryTrackingBehavior.NoTrackingWithIdentityResolution)
                return;

            var instanceId = dbContext.ContextId.InstanceId;

            //已存在就不添加
            if (!_dbContexts.TryAdd(instanceId, dbContext))
                return;

            //保存失败后
            dbContext.SaveChangesFailed += OnDbContextOnSaveChangesFailed;
        }

        /// <summary>
        /// 上下文保存失败事件
        /// </summary>
        /// <param name="s">事件触发对象</param>
        /// <param name="e">事件类型</param>
        private static async void OnDbContextOnSaveChangesFailed(object s, SaveChangesFailedEventArgs e)
        {
            //100% DbContext
            var context = s as DbContext;

            var database = context.Database;

            //事务共享，为null则都不存在
            var transaction = database.CurrentTransaction;
            if (transaction == null)
                return;

            //避免重复添加
            if (!FailedDbContexts.TryAdd(context.ContextId.InstanceId, context))
                return;

            //回滚
            await transaction.RollbackAsync();
        }

        /// <summary>
        /// 保存更新
        /// </summary>
        /// <returns></returns>
        public async Task<int> SaveChangesAsync()
        {
            var tasks = _dbContexts.Where(x => x.Value != null && !FailedDbContexts.Contains(x))
                .Select(x => x.Value.SaveChangesAsync());

            var result = await Task.WhenAll(tasks);
            return result.Sum();
        }

        public int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            var result = _dbContexts.Where(x => x.Value != null && !FailedDbContexts.Contains(x))
                .Select(x => x.Value.SaveChanges(acceptAllChangesOnSuccess));

            return result.Sum();
        }

        public int SaveChanges()
        {
            var result = _dbContexts.Where(x => x.Value != null && !FailedDbContexts.Contains(x))
                .Select(x => x.Value.SaveChanges());

            return result.Sum();
        }

        /// <summary>
        /// 保存更新
        /// </summary>
        /// <param name="acceptAllChangesOnSuccess"></param>
        /// <returns></returns>
        public async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess)
        {
            var tasks = _dbContexts.Where(x => x.Value != null && !FailedDbContexts.Contains(x))
                .Select(x => x.Value.SaveChangesAsync(acceptAllChangesOnSuccess));

            var result = await Task.WhenAll(tasks);
            return result.Sum();
        }

        /// <summary>
        /// 开启事务
        /// </summary>
        /// <returns></returns>
        public async Task<IDbContextContainer> BeginTransactionAsync()
        {
            if (!_dbContexts.Any()) return this;

            if (DbContextTransaction == null)
            {
                //获取第一个上下文事务不为空的实例
                var (_, dbContext) = _dbContexts.FirstOrDefault(x => x.Value.Database.CurrentTransaction != null);

                DbContextTransaction = dbContext != null
                    ? dbContext.Database.CurrentTransaction
                    : await _dbContexts.First().Value.Database.BeginTransactionAsync();

                await ShareTransactionAsync(DbContextTransaction.GetDbTransaction());
            }

            //开始运行
            IsRunning = true;
            return this;
        }

        /// <summary>
        /// 开启事务
        /// </summary>
        /// <returns></returns>
        public IDbContextContainer BeginTransaction()
        {
            if (!_dbContexts.Any()) return this;

            if (DbContextTransaction == null)
            {
                //获取第一个上下文事务不为空的实例
                var (_, dbContext) = _dbContexts.FirstOrDefault(x => x.Value.Database.CurrentTransaction != null);

                DbContextTransaction = dbContext != null
                    ? dbContext.Database.CurrentTransaction
                    : _dbContexts.First().Value.Database.BeginTransaction();

                ShareTransaction(DbContextTransaction.GetDbTransaction());
            }

            //开始运行
            IsRunning = true;
            return this;
        }


        /// <summary>
        /// 提交事务
        /// </summary>
        /// <param name="acceptAllChangesOnSuccess"></param>
        /// <param name="exception"></param>
        public async Task CommitTransactionAsync(bool acceptAllChangesOnSuccess, Exception exception = null)
        {
            //存在异常
            if (exception != null)
            {
                //回滚事务 
                if (DbContextTransaction?.GetDbTransaction().Connection != null)
                    await DbContextTransaction.RollbackAsync();
            }
            else
            {
                //提交事务
                try
                {
                    //如果指定提交,则直接执行
                    if (!IsAutomaticSaveChanges)
                    {
                        await SaveChangesAsync(acceptAllChangesOnSuccess);
                    }

                    if (DbContextTransaction != null)
                    {
                        await DbContextTransaction.CommitAsync();
                    }
                }
                catch
                {
                    // 回滚事务
                    if (DbContextTransaction?.GetDbTransaction()?.Connection != null)
                        await DbContextTransaction.RollbackAsync();

                    //原始错误
                    throw;
                }
            }
        }

        public void CommitTransaction(bool acceptAllChangesOnSuccess, Exception exception = null)
        {
            //存在异常
            if (exception != null)
            {
                //回滚事务 
                if (DbContextTransaction?.GetDbTransaction().Connection != null)
                    DbContextTransaction.Rollback();
            }
            else
            {
                //提交事务
                try
                {
                    if (!IsAutomaticSaveChanges)
                    {
                        SaveChanges(acceptAllChangesOnSuccess);
                    }

                    DbContextTransaction?.Commit();
                }
                catch
                {
                    // 回滚事务
                    if (DbContextTransaction?.GetDbTransaction()?.Connection != null)
                        DbContextTransaction.Rollback();

                    //原始错误
                    throw;
                }
            }
        }


        /// <summary>
        /// 共享上下文事务
        /// </summary>
        /// <param name="transaction"></param>
        private async Task ShareTransactionAsync(DbTransaction transaction)
        {
            var dic = _dbContexts.Where(x => x.Value != null && x.Value.Database.CurrentTransaction == null);
            foreach (var kv in dic)
            {
                var db = kv.Value.Database;
                await db.UseTransactionAsync(transaction);
            }
        }

        /// <summary>
        /// 共享上下文事务
        /// </summary>
        /// <param name="transaction"></param>
        private void ShareTransaction(DbTransaction transaction)
        {
            var dic = _dbContexts.Where(x => x.Value != null && x.Value.Database.CurrentTransaction == null);
            foreach (var kv in dic)
            {
                var db = kv.Value.Database;
                db.UseTransaction(transaction);
            }
        }
    }
}