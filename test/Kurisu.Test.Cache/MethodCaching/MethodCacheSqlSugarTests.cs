using AspectCore.Extensions.DependencyInjection;
using Kurisu.AspNetCore.Abstractions.Cache;
using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.AspNetCore.Abstractions.DistributedLock;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.Extensions.Cache;
using Kurisu.Extensions.SqlSugar;
using Kurisu.Extensions.SqlSugar.Context;
using Kurisu.Extensions.SqlSugar.Options;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using Xunit;

namespace Kurisu.Test.Cache.MethodCaching;

public class MethodCacheSqlSugarTests
{
    private static ServiceProvider Build(TestCache cache, QueryWork work)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<DbOptions>(o =>
        {
            o.DefaultConnectionString = "Data Source=:memory:";
            o.AdditionalConnectionStrings = new();
            o.Timeout = 30;
        });
        services.AddSqlSugar(DbType.Sqlite);
        services.AddSingleton<ICache>(cache);
        services.AddSingleton<ILockable>(new TestLocks());
        services.AddSingleton(work);
        services.AddMethodCaching();
        services.AddTransient<IQueryService, QueryService>();
        return services.BuildDynamicProxyProvider();
    }

    [Fact]
    public async Task RealSqlSugarTransactions_RequiredCommitAndRollbackControlEviction()
    {
        var cache = new TestCache();
        var work = new QueryWork();
        using var provider = Build(cache, work);
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        using var lifecycle = sp.InitLifecycle();
        var service = sp.GetRequiredService<IQueryService>();
        var db = sp.GetRequiredService<IDbContext>();
        var callbacks = sp.GetRequiredService<ITransactionCallbackRegistry>();
        using var datasource = db.CreateDatasourceScope();
        using var tenant = db.UseTenant("tenant-a");
        await service.GetAsync(1);
        Assert.Single(cache.Data);
        using (var outer = db.DatasourceManager.CreateTransScope(Propagation.Required))
        {
            await outer.BeginAsync();
            Assert.True(callbacks.HasActiveTransaction);
            using (var inner = db.DatasourceManager.CreateTransScope(Propagation.Required))
            {
                await inner.BeginAsync();
                await service.UpdateAsync(1);
                await inner.CommitAsync();
            }
            Assert.Single(cache.Data);
            await service.GetAsync(1);
            Assert.Equal(2, work.Calls);
            await outer.RollbackAsync();
        }
        Assert.False(callbacks.HasActiveTransaction);
        await service.GetAsync(1);
        Assert.Equal(2, work.Calls);
        using (var transaction = db.DatasourceManager.CreateTransScope(Propagation.Required))
        {
            await transaction.BeginAsync();
            await service.UpdateAsync(1);
            Assert.Single(cache.Data);
            await transaction.CommitAsync();
        }
        Assert.Empty(cache.Data);
        Assert.False(callbacks.HasActiveTransaction);
    }

    [Fact]
    public async Task EffectiveTenant_IsolatesCacheAndEvictionAcrossNestedScopes()
    {
        var cache = new TestCache();
        var work = new QueryWork();
        using var provider = Build(cache, work);
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        using var lifecycle = sp.InitLifecycle();
        var service = sp.GetRequiredService<IQueryService>();
        var db = sp.GetRequiredService<IDbContext>();
        using var datasource = db.CreateDatasourceScope();
        using (db.UseTenant("outer"))
        {
            await service.GetAsync(1);
            using (db.UseTenant("inner"))
            {
                Assert.Equal("inner", sp.GetRequiredService<IDbTenantAccessor>().GetTenantId());
                await service.GetAsync(1);
                await service.UpdateAsync(1);
                await service.GetAsync(1);
                Assert.Equal(3, work.Calls);
            }
            Assert.Equal("outer", sp.GetRequiredService<IDbTenantAccessor>().GetTenantId());
            await service.GetAsync(1);
            Assert.Equal(3, work.Calls);
            Assert.Equal(2, cache.Data.Count);
        }
    }
}
