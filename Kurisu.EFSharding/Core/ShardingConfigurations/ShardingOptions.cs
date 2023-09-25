﻿using System.Data.Common;
using Kurisu.EFSharding.Core.DependencyInjection;
using Kurisu.EFSharding.Sharding.ReadWriteConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Kurisu.EFSharding.Core.ShardingConfigurations;

/// <summary>
/// 分片配置
/// </summary>
public class ShardingOptions
{
    /// <summary>
    /// 模型缓存锁等待时间
    /// </summary>
    public int CacheModelLockObjectSeconds { get; set; } = 3;

    /// <summary>
    /// 模型缓存的优先级
    /// </summary>
    public CacheItemPriority CacheItemPriority => CacheItemPriority.High;

    /// <summary>
    /// efcore缓存最多限制10240个，单个缓存size设置为10那么就意味可以最多统一时间缓存1024个(缓存过期了那么还是会可以缓存进去的)
    /// </summary>
    public int CacheEntrySize { get; set; } = 1;

    /// <summary>
    /// 模型缓存锁等级
    /// </summary>
    public int CacheModelLockConcurrencyLevel { get; set; } = 1;

    /// <summary>
    /// 是否使用代理模式
    /// </summary>
    public bool UseEntityFrameworkCoreProxies { get; set; }

    /// <summary>
    /// 配置全局迁移最大并行数,以data source为一个单元并行迁移保证在多数据库分库情况下可以大大提高性能
    /// 默认系统逻辑处理器<code>Environment.ProcessorCount</code>
    /// </summary>
    public int MigrationParallelCount { get; set; } = Environment.ProcessorCount;


    /// <summary>
    /// 全局配置最大的查询连接数限制,默认系统逻辑处理器<code>Environment.ProcessorCount</code>
    /// </summary>
    public int MaxQueryConnectionsLimit { get; set; } = Environment.ProcessorCount;


    /// <summary>
    /// 读写分离配置
    /// </summary>
    public ShardingReadWriteSeparationOptions ShardingReadWriteSeparationOptions { get; private set; }


    /// <summary>
    /// 添加数据源
    /// </summary>
    /// <param name="datasourceConfigure"></param>
    public void AddDatasource(Func<IShardingProvider, List<DatasourceUnit>> datasourceConfigure)
    {
        DatasourceConfigure = datasourceConfigure;
    }

    public Func<IShardingProvider, List<DatasourceUnit>> DatasourceConfigure { get; private set; }


    /// <summary>
    /// 添加读写分离配置
    /// </summary>
    /// <param name="readWriteSeparationConfigure"></param>
    /// <param name="readStrategyEnum">随机或者轮询</param>
    /// <param name="defaultEnableBehavior"></param>
    /// <param name="defaultEnable">DefaultDisable表示哪怕您添加了读写分离也不会进行读写分离查询,只有需要的时候自行开启,DefaultEnable表示默认查询就是走的读写分离,InTransactionEnable在事务中的查询使用读写分离,InTransactionDisbale在事务中不使用读写分离</param>
    /// <param name="defaultPriority">默认优先级建议大于0</param>
    /// <param name="readConnStringGetStrategy">LatestFirstTime:DbContext缓存,LatestEveryTime:每次都是最新</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void AddReadWriteSeparation(
        Func<IShardingProvider, IDictionary<string, IEnumerable<string>>> readWriteSeparationConfigure,
        ReadStrategyEnum readStrategyEnum,
        ReadWriteDefaultEnableBehavior defaultEnableBehavior = ReadWriteDefaultEnableBehavior.DefaultDisable,
        int defaultPriority = 10,
        ReadConnStringGetStrategyEnum readConnStringGetStrategy = ReadConnStringGetStrategyEnum.LatestFirstTime)
    {
        ShardingReadWriteSeparationOptions = new ShardingReadWriteSeparationOptions();
        ShardingReadWriteSeparationOptions.ReadWriteSeparationConfigure = readWriteSeparationConfigure ?? throw new ArgumentNullException(nameof(readWriteSeparationConfigure));
        ShardingReadWriteSeparationOptions.ReadStrategy = readStrategyEnum;
        ShardingReadWriteSeparationOptions.DefaultEnableBehavior = defaultEnableBehavior;
        ShardingReadWriteSeparationOptions.DefaultPriority = defaultPriority;
        ShardingReadWriteSeparationOptions.ReadConnStringGetStrategy = readConnStringGetStrategy;
    }

    /// <summary>
    /// 读写分离配置 和 AddReadWriteSeparation不同的是
    /// 当前配置支持自定义读链接节点命名,命名的好处在于当使用读库链接的时候由于服务器性能的差异
    /// 可以将部分吃性能的查询通过节点名称切换到对应的性能相对较好或者较空闲的读库服务器
    /// <code><![CDATA[
    /// IShardingReadWriteManager _readWriteManager=...
    ///   using (_readWriteManager.CreateScope())
    ///     {
    ///         _readWriteManager.GetCurrent().SetReadWriteSeparation(100,true);
    ///         _readWriteManager.GetCurrent().AdddatasourceReadNode("A", readNodeName);
    ///         var xxxaaa = await _defaultTableDbContext.Set<SysUserSalary>().FirstOrDefaultAsync();
    ///   }]]></code>
    /// </summary>
    /// <param name="readWriteNodeSeparationConfigure"></param>
    /// <param name="readStrategyEnum"></param>
    /// <param name="defaultEnableBehavior"></param>
    /// <param name="defaultPriority"></param>
    /// <param name="readConnStringGetStrategy"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void AddReadWriteNodeSeparation(
        Func<IShardingProvider, IDictionary<string, IEnumerable<ReadNode>>> readWriteNodeSeparationConfigure,
        ReadStrategyEnum readStrategyEnum,
        ReadWriteDefaultEnableBehavior defaultEnableBehavior = ReadWriteDefaultEnableBehavior.DefaultDisable,
        int defaultPriority = 10,
        ReadConnStringGetStrategyEnum readConnStringGetStrategy = ReadConnStringGetStrategyEnum.LatestFirstTime)
    {
        ShardingReadWriteSeparationOptions = new ShardingReadWriteSeparationOptions();
        ShardingReadWriteSeparationOptions.ReadWriteNodeSeparationConfigure = readWriteNodeSeparationConfigure ?? throw new ArgumentNullException(nameof(readWriteNodeSeparationConfigure));
        ShardingReadWriteSeparationOptions.ReadStrategy = readStrategyEnum;
        ShardingReadWriteSeparationOptions.DefaultEnableBehavior = defaultEnableBehavior;
        ShardingReadWriteSeparationOptions.DefaultPriority = defaultPriority;
        ShardingReadWriteSeparationOptions.ReadConnStringGetStrategy = readConnStringGetStrategy;
    }

    /// <summary>
    /// 多个DbContext事务传播委托
    /// </summary>
    public Action<DbConnection, DbContextOptionsBuilder> ConnectionConfigure { get; private set; }

    /// <summary>
    /// 初始DbContext的创建委托
    /// </summary>
    public Action<string, DbContextOptionsBuilder> ConnectionStringConfigure { get; private set; }

    /// <summary>
    /// 外部dbcontext的配置委托
    /// </summary>
    public Action<DbContextOptionsBuilder> ShellDbContextConfigure { get; private set; }

    /// <summary>
    /// 仅内部真正执行的DbContext生效的配置委托
    /// </summary>
    public Action<DbContextOptionsBuilder> ExecutorDbContextConfigure { get; private set; }

    /// <summary>
    /// 分片迁移使用的配置
    /// </summary>

    public Action<DbContextOptionsBuilder> ShardingMigrationConfigure { get; private set; }

    /// <summary>
    /// 添加分片迁移的配置
    /// 当前配置只有在调用迁移代码时才会生效
    /// <code><![CDATA[
    /// using (var scope = app.ApplicationServices.CreateScope())
    /// {
    ///   var defaultShardingDbContext = scope.ServiceProvider.GetService<DefaultShardingDbContext>();
    ///    defaultShardingDbContext.Database.Migrate();
    /// }
    /// ]]></code>
    /// </summary>
    /// <param name="configure"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void UseShardingMigrationConfigure(Action<DbContextOptionsBuilder> configure)
    {
        ShardingMigrationConfigure = configure ?? throw new ArgumentNullException(nameof(configure));
    }

    /// <summary>
    /// 如何使用字符串创建DbContext
    /// </summary>
    /// <param name="queryConfigure"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void UseShardingQuery(Action<string, DbContextOptionsBuilder> queryConfigure)
    {
        ConnectionStringConfigure = queryConfigure ?? throw new ArgumentNullException(nameof(queryConfigure));
    }

    /// <summary>
    /// 传递事务到其他DbContext
    /// </summary>
    /// <param name="transactionConfigure"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void UseShardingTransaction(Action<DbConnection, DbContextOptionsBuilder> transactionConfigure)
    {
        ConnectionConfigure = transactionConfigure ?? throw new ArgumentNullException(nameof(transactionConfigure));
    }

    /// <summary>
    /// 仅内部正真执行DbContext生效,作为最外面的壳DbContext将不会生效
    /// </summary>
    /// <param name="executorDbContextConfigure"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void UseExecutorDbContextConfigure(Action<DbContextOptionsBuilder> executorDbContextConfigure)
    {
        ExecutorDbContextConfigure = executorDbContextConfigure ?? throw new ArgumentNullException(nameof(executorDbContextConfigure));
    }

    /// <summary>
    /// 仅外部DbContext生效,如果是独立调用AddDbContext和AddShardingConfigure不一定生效
    /// 会在AddShardingDbContext里面自动赋值
    /// </summary>
    /// <param name="shellDbContextConfigure"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void UseShellDbContextConfigure(Action<DbContextOptionsBuilder> shellDbContextConfigure)
    {
        ShellDbContextConfigure = shellDbContextConfigure ?? throw new ArgumentNullException(nameof(shellDbContextConfigure));
    }
}