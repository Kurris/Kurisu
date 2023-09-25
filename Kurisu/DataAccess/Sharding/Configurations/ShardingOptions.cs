using System;
using System.Collections.Generic;
using System.Data.Common;
using Kurisu.DataAccess.Sharding.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Kurisu.DataAccess.Sharding.Configurations;

public class ShardingOptions
{
    /// <summary>
    /// 模型缓存锁等待时间
    /// </summary>
    public int CacheModelLockObjectSeconds { get; set; } = 3;

    /// <summary>
    /// 模型缓存的优先级
    /// </summary>
    public CacheItemPriority CacheItemPriority { get; set; } = CacheItemPriority.High;

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
    public bool UseEntityFrameworkCoreProxies { get; set; } = false;

    /// <summary>
    /// 写操作数据库后自动使用写库链接防止读库链接未同步无法查询到数据
    /// </summary>
    public bool AutoUseWriteConnectionStringAfterWriteDb { get; set; } = false;

    /// <summary>
    /// 当查询遇到没有路由被命中时是否抛出错误
    /// </summary>
    public bool ThrowIfQueryRouteNotMatch { get; set; } = true;

    /// <summary>
    /// 忽略建表时的错误
    /// </summary>
    public bool? IgnoreCreateTableError { get; set; } = false;

    /// <summary>
    /// 配置全局迁移最大并行数,以data source为一个单元并行迁移保证在多数据库分库情况下可以大大提高性能
    /// 默认系统逻辑处理器<code>Environment.ProcessorCount</code>
    /// </summary>
    public int MigrationParallelCount { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// 启动补偿表的最大并行数,以data source为一个单元并行迁移保证在多数据库分库情况下可以大大提高性能
    /// 默认系统逻辑处理器<code>Environment.ProcessorCount</code>
    /// </summary>
    public int CompensateTableParallelCount { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// 全局配置最大的查询连接数限制,默认系统逻辑处理器<code>Environment.ProcessorCount</code>
    /// </summary>
    public int MaxQueryConnectionsLimit { get; set; } = Environment.ProcessorCount;


    /// <summary>
    /// 默认数据源
    /// </summary>
    public string DefaultDataSourceName { get; private set; }

    /// <summary>
    /// 默认数据源链接字符串
    /// </summary>
    public string DefaultConnectionString { get; private set; }

    /// <summary>
    /// 检测分片键的自动值是否有疑义
    /// </summary>

    public bool CheckShardingKeyValueGenerated { get; set; } = true;

    /// <summary>
    /// 添加默认数据源
    /// </summary>
    /// <param name="dataSourceName"></param>
    /// <param name="connectionString"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void AddDefaultDataSource(string dataSourceName, string connectionString)
    {
        DefaultDataSourceName = dataSourceName ?? throw new ArgumentNullException(nameof(dataSourceName));
        DefaultConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public Func<IShardingProvider, IDictionary<string, string>> DataSourcesConfigure { get; private set; }

    /// <summary>
    /// 添加额外数据源
    /// </summary>
    /// <param name="extraDataSourceConfigure"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void AddExtraDataSource(Func<IShardingProvider, IDictionary<string, string>> extraDataSourceConfigure)
    {
        DataSourcesConfigure = extraDataSourceConfigure ?? throw new ArgumentNullException(nameof(extraDataSourceConfigure));
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
    /// 如何传递事务到其他DbContext
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