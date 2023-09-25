﻿using Kurisu.EFSharding.Core.DbContextCreator;
using Kurisu.EFSharding.Core.RuntimeContexts;
using Kurisu.EFSharding.Core.VirtualDatabase.VirtualDatasource;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Infrastructures;
using Kurisu.EFSharding.Extensions;
using Kurisu.EFSharding.Extensions.DbContextExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kurisu.EFSharding.Sharding.ShardingDbContextExecutors;

public class DataSourceDbContext : IDatasourceDbContext
{
    private static readonly IComparer<string> Comparer = new NoShardingFirstComparer();

    public Type DbContextType { get; }


    public bool IsDefault { get; }

    /// <summary>
    /// 当前同库有多少dbcontext了
    /// </summary>
    public int DbContextCount => _dataSourceDbContexts.Count;

    /// <summary>
    /// dbcontext 创建接口
    /// </summary>
    private readonly IDbContextCreator _dbContextCreator;

    /// <summary>
    /// 实际的链接字符串管理者 用来提供查询和插入dbcontext的创建链接的获取
    /// </summary>
    private readonly ActualConnectionStringManager _actualConnectionStringManager;

    /// <summary>
    /// 当前的数据源是什么默认单数据源可以支持多数据源配置
    /// </summary>
    private readonly IVirtualDatasource _virtualDataSource;

    /// <summary>
    /// 数据源名称
    /// </summary>
    public string DataSourceName { get; }

    /// <summary>
    /// 数据源排序默认提交将未分片的数据库最先提交
    /// </summary>
    private SortedDictionary<string, DbContext> _dataSourceDbContexts =
        new SortedDictionary<string, DbContext>(Comparer);

    /// <summary>
    /// 是否开启了事务
    /// </summary>
    private bool _isBeginTransaction => _shardingShellDbContext.Database.CurrentTransaction != null;

    /// <summary>
    /// shell dbcontext最外面的壳
    /// </summary>
    private readonly DbContext _shardingShellDbContext;

    private readonly IShardingRuntimeContext _shardingRuntimeContext;

    /// <summary>
    /// 数据库事务
    /// </summary>
    private IDbContextTransaction _shardingContextTransaction =>
        _shardingShellDbContext?.Database?.CurrentTransaction;

    /// <summary>
    /// 同库下公用一个db context options
    /// </summary>
    private DbContextOptions _dbContextOptions;

    /// <summary>
    /// 是否触发了并发如果是的话就报错
    /// </summary>
    private OneByOneChecker oneByOne = new();

    private IDbContextTransaction CurrentDbContextTransaction => IsDefault
        ? _shardingContextTransaction
        : _dataSourceDbContexts.Values.FirstOrDefault(o => o.Database.CurrentTransaction != null)?.Database
            ?.CurrentTransaction;

    /// <summary>
    ///
    /// </summary>
    /// <param name="dataSourceName"></param>
    /// <param name="isDefault"></param>
    /// <param name="shardingShellDbContext"></param>
    /// <param name="dbContextCreator"></param>
    /// <param name="actualConnectionStringManager"></param>
    public DataSourceDbContext(string dataSourceName,
        bool isDefault,
        DbContext shardingShellDbContext,
        IDbContextCreator dbContextCreator,
        ActualConnectionStringManager actualConnectionStringManager)
    {
        DataSourceName = dataSourceName;
        IsDefault = isDefault;
        _shardingShellDbContext = shardingShellDbContext;
        _shardingRuntimeContext = shardingShellDbContext.GetShardingRuntimeContext();
        DbContextType = shardingShellDbContext.GetType();
        _virtualDataSource = _shardingRuntimeContext.GetVirtualDatasource();
        _dbContextCreator = dbContextCreator;
        _actualConnectionStringManager = actualConnectionStringManager;
    }

    /// <summary>
    /// 创建共享的数据源配置用来做事务 不支持并发后期发现直接报错
    /// </summary>
    /// <returns></returns>
    private DbContextOptions CreateShareDbContextOptionsBuilder()
    {
        if (_dbContextOptions != null)
        {
            return _dbContextOptions;
        }

        //是否触发并发了
        var acquired = oneByOne.Start();
        if (!acquired)
        {
            throw new ShardingCoreException("cant parallel create CreateShareDbContextOptionsBuilder");
        }

        try
        {
            //先创建dbcontext option builder
            var dbContextOptionBuilderCreator = _shardingRuntimeContext.GetDbContextOptionBuilderCreator();
            var dbContextOptionsBuilder = dbContextOptionBuilderCreator.CreateDbContextOptionBuilder()
                .UseShardingOptions(_shardingRuntimeContext);

            if (IsDefault)
            {
                //如果是默认的需要使用shell的dbconnection为了保证可以使用事务
                var dbConnection = _shardingShellDbContext.Database.GetDbConnection();
                _virtualDataSource.UseDbContextOptionsBuilder(dbConnection,
                    dbContextOptionsBuilder);
            }
            else
            {
                //不同数据库下的链接需要自行获取 如果当前没有dbcontext那么就是第一个,应该用链接字符串创建后续的用dbconnection创建
                if (_dataSourceDbContexts.IsEmpty())
                {
                    var connectionString =
                        _actualConnectionStringManager.GetConnectionString(DataSourceName, true);
                    _virtualDataSource.UseDbContextOptionsBuilder(connectionString,
                        dbContextOptionsBuilder);
                    return dbContextOptionsBuilder.Options;
                }
                else
                {
                    var dbConnection = _dataSourceDbContexts.First().Value.Database.GetDbConnection();
                    _virtualDataSource.UseDbContextOptionsBuilder(dbConnection,
                        dbContextOptionsBuilder);
                }
            }

            _dbContextOptions = dbContextOptionsBuilder.Options;
            return _dbContextOptions;
        }
        finally
        {
            oneByOne.Stop();
        }
    }

    /// <summary>
    /// 不支持并发后期发现直接报错而不是用lock
    /// </summary>
    /// <param name="routeTail"></param>
    /// <returns></returns>
    public DbContext CreateDbContext(IRouteTail routeTail)
    {
        if (routeTail.IsMultiEntityQuery())
            throw new NotSupportedException("multi route not support track");
        if (routeTail is not ISingleQueryRouteTail singleQueryRouteTail)
            throw new NotSupportedException("multi route not support track");

        var cacheKey = routeTail.GetRouteTailIdentity();
        if (!_dataSourceDbContexts.TryGetValue(cacheKey, out var dbContext))
        {
            dbContext = _dbContextCreator.CreateDbContext(_shardingShellDbContext,
                CreateShareDbContextOptionsBuilder(), routeTail);
            _dataSourceDbContexts.Add(cacheKey, dbContext);
            ShardingDbTransaction();
        }

        return dbContext;
    }

    private void ShardingDbTransaction()
    {
        if (_isBeginTransaction)
        {
            BeginAnyTransaction();
            JoinCurrentTransaction();
        }
    }

    /// <summary>
    /// 加入到当前事务
    /// </summary>
    private void JoinCurrentTransaction()
    {
        //如果当前的dbcontext有的话
        if (CurrentDbContextTransaction != null)
        {
            var dbTransaction = CurrentDbContextTransaction.GetDbTransaction();
            foreach (var dataSourceDbContext in _dataSourceDbContexts)
            {
                if (dataSourceDbContext.Value.Database.CurrentTransaction == null)
                    dataSourceDbContext.Value.Database.UseTransaction(dbTransaction);
            }
        }
    }

    private void BeginAnyTransaction()
    {
        if (_isBeginTransaction)
        {
            if (!IsDefault)
            {
                if (!_dataSourceDbContexts.IsEmpty())
                {
                    var isolationLevel = _shardingContextTransaction.GetDbTransaction().IsolationLevel;
                    var firstTransaction = _dataSourceDbContexts.Values
                        .FirstOrDefault(o => o.Database.CurrentTransaction != null)
                        ?.Database?.CurrentTransaction;
                    if (firstTransaction == null)
                    {
                        _ = _dataSourceDbContexts.First().Value.Database
                            .BeginTransaction(isolationLevel);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 通知事务自动管理是否要清理还是开启还是加入事务
    /// </summary>
    public void NotifyTransaction()
    {
        if (!_isBeginTransaction)
        {
            ClearTransaction();
        }
        else
        {
            BeginAnyTransaction();
            JoinCurrentTransaction();
        }
    }

    /// <summary>
    /// 清理事务
    /// </summary>
    private void ClearTransaction()
    {
        foreach (var dataSourceDbContext in _dataSourceDbContexts)
        {
            if (dataSourceDbContext.Value.Database.CurrentTransaction != null)
                dataSourceDbContext.Value.Database.UseTransaction(null);
        }
    }

    /// <summary>
    /// 提交
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess"></param>
    /// <returns></returns>
    public int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        int i = 0;
        foreach (var dataSourceDbContext in _dataSourceDbContexts)
        {
            i += dataSourceDbContext.Value.SaveChanges(acceptAllChangesOnSuccess);
        }

        return i;
    }

    /// <summary>
    /// 异步提交
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = new CancellationToken())
    {
        int i = 0;
        foreach (var dataSourceDbContext in _dataSourceDbContexts)
        {
            i += await dataSourceDbContext.Value.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        return i;
    }

    /// <summary>
    /// 获取当前的后缀数据库字典数据
    /// </summary>
    /// <returns></returns>
    public IDictionary<string, DbContext> GetCurrentContexts()
    {
        return _dataSourceDbContexts;
    }

    /// <summary>
    /// 回滚数据
    /// </summary>
    public void Rollback()
    {
        if (IsDefault)
            return;
        CurrentDbContextTransaction?.Rollback();
    }

    /// <summary>
    /// 提交数据
    /// </summary>
    public void Commit()
    {
        if (IsDefault)
            return;
        CurrentDbContextTransaction?.Commit();
    }
#if !EFCORE2
    public async Task RollbackAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsDefault)
            return;
        if (CurrentDbContextTransaction != null)
            await CurrentDbContextTransaction.RollbackAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken =
        new CancellationToken())
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsDefault)
            return;
        if (CurrentDbContextTransaction != null)
            await CurrentDbContextTransaction.CommitAsync(cancellationToken);
    }
#if !EFCORE3&&!NETSTANDARD2_0
    public void CreateSavepoint(string name)
    {
        if (IsDefault)
            return;
        CurrentDbContextTransaction?.CreateSavepoint(name);
    }

    public async Task CreateSavepointAsync(string name,
        CancellationToken cancellationToken = new CancellationToken())
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsDefault)
            return;
        if (CurrentDbContextTransaction != null)
            await CurrentDbContextTransaction.CreateSavepointAsync(name, cancellationToken);
    }

    public void RollbackToSavepoint(string name)
    {
        if (IsDefault)
            return;
        CurrentDbContextTransaction?.RollbackToSavepoint(name);
    }

    public async Task RollbackToSavepointAsync(string name,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsDefault)
            return;
        if (CurrentDbContextTransaction != null)
            await CurrentDbContextTransaction.RollbackToSavepointAsync(name, cancellationToken);
    }

    public void ReleaseSavepoint(string name)
    {
        if (IsDefault)
            return;
        CurrentDbContextTransaction?.ReleaseSavepoint(name);
    }

    public async Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsDefault)
            return;
        if (CurrentDbContextTransaction != null)
            await CurrentDbContextTransaction.ReleaseSavepointAsync(name, cancellationToken);
    }
#endif
#endif

    public void Dispose()
    {
        foreach (var dataSourceDbContext in _dataSourceDbContexts)
        {
            dataSourceDbContext.Value.Dispose();
        }

        _dataSourceDbContexts.Clear();
    }
#if !EFCORE2
    public async ValueTask DisposeAsync()
    {
        foreach (var dataSourceDbContext in _dataSourceDbContexts)
        {
            await dataSourceDbContext.Value.DisposeAsync();
        }

        _dataSourceDbContexts.Clear();
    }
#endif
}