using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kurisu.DataAccessor.Internal
{
    /// <summary>
    /// 数据库上下文容器
    /// </summary>
    public class DbContextContainer : IDbContextContainer
    {
        /// <summary>
        /// 有效上下文
        /// </summary>
        private readonly ConcurrentDictionary<Guid, DbContext> _dbContexts;

        /// <summary>
        /// 失败上下文
        /// </summary>
        private readonly ConcurrentDictionary<Guid, DbContext> _failedDbContexts;


        public DbContextContainer()
        {
            _dbContexts = new ConcurrentDictionary<Guid, DbContext>();
            _failedDbContexts = new ConcurrentDictionary<Guid, DbContext>();
        }


        /// <summary>
        /// 数据库上下文事务
        /// </summary>
        public IDbContextTransaction DbContextTransaction { get; set; }


        /// <summary>
        /// 获取所有有效上下文
        /// </summary>
        /// <returns></returns>
        public ConcurrentDictionary<Guid, DbContext> GetDbContexts() => this._dbContexts;

        /// <summary>
        /// 添加上下文到容器中
        /// </summary>
        /// <param name="dbContext"></param>
        public void Add(DbContext dbContext)
        {
            //非关系型数据库
            if (!dbContext.Database.IsRelational()) return;
            if (dbContext.ChangeTracker.QueryTrackingBehavior is QueryTrackingBehavior.NoTracking
                or QueryTrackingBehavior.NoTrackingWithIdentityResolution) return;

            var instanceId = dbContext.ContextId.InstanceId;
            //已存在就不添加
            if (!this._dbContexts.TryAdd(instanceId, dbContext)) return;

            //保存失败后
            dbContext.SaveChangesFailed += OnDbContextOnSaveChangesFailed;
        }

        private async void OnDbContextOnSaveChangesFailed(object s, SaveChangesFailedEventArgs e)
        {
            var contextSender = (DbContext) s;
            var failedInstanceId = contextSender.ContextId.InstanceId;
            //已存在就不添加
            if (!_failedDbContexts.TryAdd(failedInstanceId, contextSender)) return;

            var database = contextSender.Database;

            var transaction = database.CurrentTransaction;
            if (transaction == null) return;

            //用于打印信息
            var connection = database.GetDbConnection();

            //回滚
            await transaction.RollbackAsync();
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var tasks = _dbContexts.Where(x => x.Value != null
                                               && !_failedDbContexts.Contains(x))
                .Select(x => x.Value.SaveChangesAsync(cancellationToken));

            var result = await Task.WhenAll(tasks);
            return result == null
                ? 0
                : result.Sum();
        }

        public async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            var tasks = _dbContexts.Where(x => x.Value != null
                                               && !_failedDbContexts.Contains(x))
                .Select(x => x.Value.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken));

            var result = await Task.WhenAll(tasks);
            return result.Sum();
        }

        public async Task<IDbContextContainer> BeginTransactionAsync(bool ensureTransaction = false)
        {
            if (!_dbContexts.Any()) throw new ArgumentException(nameof(_dbContexts));

            if (DbContextTransaction == null)
            {
                //获取第一个上下文事务不为空的实例
                var (_, dbContext) = _dbContexts.FirstOrDefault(x => x.Value.Database.CurrentTransaction != null);

                DbContextTransaction = dbContext != null
                    ? dbContext.Database.CurrentTransaction
                    : await _dbContexts.First().Value.Database.BeginTransactionAsync();

                await UseTransactionAsync(DbContextTransaction.GetDbTransaction());
            }

            return this;
        }


        public async Task CommitTransactionAsync(bool isManualSaveChanges, Exception exception = default)
        {
            //存在异常
            if (exception != null)
            {
                //回滚事务 
                if (DbContextTransaction != null)
                {
                    if (DbContextTransaction.GetDbTransaction().Connection != null) await DbContextTransaction.RollbackAsync();
                    await DbContextTransaction.DisposeAsync();
                    DbContextTransaction = null;
                }
            }
            else
            {
                //提交事务
                try
                {
                    //如果不是手动提交,则直接执行
                    var changesCount = !isManualSaveChanges
                        ? await SaveChangesAsync()
                        : 0;

                    if (DbContextTransaction != null)
                    {
                        await DbContextTransaction.CommitAsync();
                    }

                    await this.CloseAsync();
                }
                catch
                {
                    // 回滚事务
                    if (DbContextTransaction?.GetDbTransaction()?.Connection != null) await DbContextTransaction?.RollbackAsync();
                    throw;
                }
                finally
                {
                    if (DbContextTransaction?.GetDbTransaction()?.Connection != null)
                    {
                        await DbContextTransaction.DisposeAsync();
                        DbContextTransaction = null;
                    }
                }
            }
        }

        /// <summary>
        /// 关闭所有
        /// </summary>
        public async Task CloseAsync()
        {
            //释放上下文
            foreach (var kv in _dbContexts)
            {
                var dbContext = kv.Value;
                var dbConnection = dbContext.Database.GetDbConnection();
                if (dbConnection.State != ConnectionState.Open) continue;

                await dbConnection.CloseAsync();
                await dbConnection.DisposeAsync();
                await dbContext.DisposeAsync();
            }

            //释放事务
            if (DbContextTransaction != null)
            {
                await DbContextTransaction.DisposeAsync();
                DbContextTransaction = null;
            }
        }


        /// <summary>
        /// 共享上下文事务
        /// </summary>
        /// <param name="transaction"></param>
        private async Task UseTransactionAsync(DbTransaction transaction)
        {
            foreach (var kv in _dbContexts.Where(x => x.Value != null
                                                      && x.Value.Database.CurrentTransaction == null))
            {
                var db = kv.Value.Database;
                await db.UseTransactionAsync(transaction);
            }
        }
    }
}