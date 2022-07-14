using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq;
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
            //排除只读
            if (dbContext.ChangeTracker.QueryTrackingBehavior is QueryTrackingBehavior.NoTracking or QueryTrackingBehavior.NoTrackingWithIdentityResolution)
                return;

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

            //事务共享，为null则都不存在
            var transaction = database.CurrentTransaction;
            if (transaction == null) return;

            //回滚
            await transaction.RollbackAsync();
        }

        public async Task<int> SaveChangesAsync()
        {
            var tasks = _dbContexts.Where(x => x.Value != null && !_failedDbContexts.Contains(x))
                .Select(x => x.Value.SaveChangesAsync());

            var result = await Task.WhenAll(tasks);
            return result.Sum();
        }

        public async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess)
        {
            var tasks = _dbContexts.Where(x => x.Value != null && !_failedDbContexts.Contains(x))
                .Select(x => x.Value.SaveChangesAsync(acceptAllChangesOnSuccess));

            var result = await Task.WhenAll(tasks);
            return result.Sum();
        }

        /// <summary>
        /// 开启事务
        /// </summary>
        /// <param name="ensureTransaction"></param>
        /// <returns></returns>
        public async Task<IDbContextContainer> BeginTransactionAsync(bool ensureTransaction = false)
        {
            if (!_dbContexts.Any())
            {
                return this;
            }

            if (DbContextTransaction == null)
            {
                //获取第一个上下文事务不为空的实例
                var (_, dbContext) = _dbContexts.FirstOrDefault(x => x.Value.Database.CurrentTransaction != null);

                DbContextTransaction = dbContext != null
                    ? dbContext.Database.CurrentTransaction
                    : await _dbContexts.First().Value.Database.BeginTransactionAsync();

                await ShareTransactionAsync(DbContextTransaction.GetDbTransaction());
            }

            return this;
        }


        /// <summary>
        /// 提交事务
        /// </summary>
        /// <param name="isManualSaveChanges"></param>
        /// <param name="exception"></param>
        public async Task CommitTransactionAsync(bool isManualSaveChanges, Exception exception = null)
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
                    //如果不是手动提交,则直接执行
                    if (!isManualSaveChanges)
                    {
                        await SaveChangesAsync();
                    }

                    await DbContextTransaction?.CommitAsync();
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

        public int Count => _dbContexts.Count;

        public async ValueTask DisposeAsync()
        {
            ////释放上下文
            //foreach (var kv in _dbContexts)
            //{
            //    var dbContext = kv.Value;
            //    var dbConnection = dbContext.Database.GetDbConnection();
            //    if (dbConnection.State != ConnectionState.Open) continue;

            //    await dbConnection.CloseAsync();
            //    await dbConnection.DisposeAsync();
            //    await dbContext.DisposeAsync();
            //}

            //_dbContexts.Clear();

            //foreach (var kv in _failedDbContexts)
            //{
            //    var dbContext = kv.Value;
            //    var dbConnection = dbContext.Database.GetDbConnection();
            //    if (dbConnection.State != ConnectionState.Open) continue;

            //    await dbConnection.CloseAsync();
            //    await dbConnection.DisposeAsync();
            //    await dbContext.DisposeAsync();
            //}

            //_failedDbContexts.Clear();

            ////释放事务
            //if (DbContextTransaction != null)
            //{
            //    await DbContextTransaction.DisposeAsync();
            //    DbContextTransaction = null;
            //}
        }
    }
}