using Kurisu.AspNetCore.Abstractions.Authentication;
using Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Kurisu.Extensions.ContextAccessor.Abstractions;
using Kurisu.Extensions.SqlSugar.Core.Context;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Extensions.SqlSugar.Sharding;


public class ShardingContext : SqlSugarDbContext
{
    private readonly IMemoryCache _memoryCache;
    private readonly IContextSnapshotManager<DbOperationState> _contextSnapshotManager;

    public ShardingContext(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();
        _contextSnapshotManager = serviceProvider.GetRequiredService<IContextSnapshotManager<DbOperationState>>();
    }

    private bool EnableSharding<T>()
    {
        if (typeof(T).IsAssignableTo(typeof(IShardingRoute)))
        {
            var opState = _contextSnapshotManager.ContextAccessor.Current;
            return !opState.IgnoreSharding;
        }

        return false;
    }

    public override ICodeFirstMode CodeFirst => new SqlsugarShardingCodeFirstMode(Client);

    public override bool Insert<T>(T obj)
    {
        if (EnableSharding<T>())
        {
            var tableName = GetShardingTableName(obj);
            return Client.Insertable(obj).AS(tableName).ExecuteCommandIdentityIntoEntity();
        }

        return base.Insert(obj);
    }

    public override async Task<bool> InsertAsync<T>(T obj, CancellationToken cancellationToken)
    {
        if (EnableSharding<T>())
        {
            var tableName = GetShardingTableName(obj);
            return await Client.Insertable(obj).AS(tableName).ExecuteCommandIdentityIntoEntityAsync();
        }

        return await base.InsertAsync(obj, cancellationToken);
    }

    public override bool Insert<T>(List<T> objs)
    {
        if (EnableSharding<T>())
        {
            var dict = GetShardingTableNames(objs);
            foreach (var kv in dict)
            {
                var tableName = kv.Key;
                var list = kv.Value;
                var flag = Client.Insertable(list).AS(tableName).ExecuteCommandIdentityIntoEntity();
                if (!flag)
                {
                    return false;
                }
            }
        }

        return base.Insert(objs);
    }


    public override async Task<bool> InsertAsync<T>(List<T> objs, CancellationToken cancellationToken)
    {
        if (objs.Count > 0)
        {
            if (EnableSharding<T>())
            {
                var dict = GetShardingTableNames(objs);
                foreach (var kv in dict)
                {
                    var tableName = kv.Key;
                    var list = kv.Value;
                    await Client.Insertable(list).AS(tableName).ExecuteCommandIdentityIntoEntityAsync();
                }

                return true;
            }

            return await base.InsertAsync(objs, cancellationToken);
        }

        return true;
    }

    public override int Update<T>(T obj)
    {
        return this.Update(obj, null);
    }

    public override int Update<T>(T obj, string[] updateColumns)
    {
        return this.Update(new List<T> { obj }, updateColumns);
    }

    public override int Update<T>(List<T> objs)
    {
        return this.Update(objs, null);
    }

    public override int Update<T>(List<T> objs, string[] updateColumns)
    {
        if (EnableSharding<T>())
        {
            var dict = GetShardingTableNames(objs);
            int total = 0;
            foreach (var kv in dict)
            {
                var tableName = kv.Key;
                var list = kv.Value;
                var count = Client.Updateable(list).AS(tableName).UpdateColumnsIF(updateColumns != null && updateColumns.Length > 0, updateColumns).ExecuteCommand();
                total += count;
            }
            return total;
        }

        return base.Update(objs, updateColumns);
    }

    public override Task<int> UpdateAsync<T>(T obj, CancellationToken cancellationToken)
    {
        return this.UpdateAsync(obj, null, cancellationToken);
    }

    public override Task<int> UpdateAsync<T>(T obj, string[] updateColumns, CancellationToken cancellationToken)
    {
        return this.UpdateAsync(new List<T> { obj }, updateColumns, cancellationToken);
    }

    public override Task<int> UpdateAsync<T>(List<T> objs, CancellationToken cancellationToken)
    {
        return this.UpdateAsync(objs, null, cancellationToken);
    }

    public override async Task<int> UpdateAsync<T>(List<T> objs, string[] updateColumns, CancellationToken cancellationToken)
    {
        if (objs.Count > 0)
        {
            if (EnableSharding<T>())
            {
                var dict = GetShardingTableNames(objs);
                int total = 0;
                foreach (var kv in dict)
                {
                    var tableName = kv.Key;
                    var list = kv.Value;
                    var count = await Client.Updateable(list).AS(tableName).UpdateColumnsIF(updateColumns != null && updateColumns.Length > 0, updateColumns).ExecuteCommandAsync(cancellationToken);
                    total += count;
                }
                return total;
            }

            return await base.UpdateAsync(objs, updateColumns, cancellationToken);
        }

        return 0;
    }

    public override int Delete<T>(T obj, bool isReally)
    {
        return this.Delete(new List<T> { obj }, isReally);
    }

    public override int Delete<T>(List<T> objs, bool isReally)
    {
        if (EnableSharding<T>())
        {
            var dict = GetShardingTableNames(objs);
            int total = 0;
            foreach (var kv in dict)
            {
                var tableName = kv.Key;
                var list = kv.Value;
                int count = 0;

                if (typeof(T).IsAssignableTo(typeof(ISoftDeleted)))
                {
                    if (isReally)
                    {
                        count = Client.Deleteable(list).AS(tableName).ExecuteCommand();
                    }
                    else
                    {
                        foreach (ISoftDeleted item in list)
                        {
                            item.IsDeleted = true;
                        }
                        count = Client.Updateable(list).AS(tableName).UpdateColumns(nameof(ISoftDeleted.IsDeleted)).ExecuteCommand();
                    }
                }
                else
                {
                    count = Client.Deleteable(list).AS(tableName).ExecuteCommand();
                }
                total += count;
            }
            return total;
        }

        return base.Delete(objs, isReally);
    }

    public override Task<int> DeleteAsync<T>(T obj, bool isReally, CancellationToken cancellationToken)
    {
        return this.DeleteAsync(new List<T> { obj }, isReally, cancellationToken);
    }

    public override async Task<int> DeleteAsync<T>(List<T> objs, bool isReally, CancellationToken cancellationToken)
    {

        if (EnableSharding<T>())
        {
            var dict = GetShardingTableNames(objs);
            int total = 0;
            foreach (var kv in dict)
            {
                var tableName = kv.Key;
                var list = kv.Value;
                int count = 0;
                if (typeof(T).IsAssignableTo(typeof(ISoftDeleted)))
                {
                    if (isReally)
                    {
                        count = await Client.Deleteable(list).AS(tableName).ExecuteCommandAsync(cancellationToken);
                    }
                    else
                    {
                        foreach (ISoftDeleted item in list)
                        {
                            item.IsDeleted = true;
                        }
                        count = await Client.Updateable(list).AS(tableName).UpdateColumns(nameof(ISoftDeleted.IsDeleted)).ExecuteCommandAsync(cancellationToken);
                    }
                }
                else
                {
                    count = await Client.Deleteable(list).AS(tableName).ExecuteCommandAsync(cancellationToken);
                }
                total += count;
            }
            return total;
        }

        return await base.DeleteAsync(objs, isReally, cancellationToken);
    }

    //*******************************************************************************************************************************************************************************

    private Dictionary<string, List<T>> GetShardingTableNames<T>(List<T> objs)
    {
        var originalTable = Client.EntityMaintenance.GetTableName<T>();

        var result = new Dictionary<string, List<T>>();
        foreach (var obj in objs)
        {
            var suffix = GetSuffix(obj);
            var tableName = originalTable + "_" + $"{suffix}";
            if (!result.TryGetValue(tableName, out List<T> value))
            {
                value = new List<T>();
                result[tableName] = value;
            }

            value.Add(obj);
        }

        return result;
    }

    private string GetShardingTableName<T>(T obj)
    {
        var originalTable = Client.EntityMaintenance.GetTableName<T>();

        var suffix = GetSuffix(obj);
        var tableName = originalTable + "_" + $"{suffix}";

        return tableName;
    }

    private string GetSuffix<T>(T obj)
    {
        if (obj is ITenantId tenant)
        {
            var currentUser = ServiceProvider.GetService<ICurrentUser>();
            var snapshotManager = ServiceProvider.GetService<IContextSnapshotManager<DbOperationState>>();

            var tenantId = tenant.TenantId;
            var opState = snapshotManager.ContextAccessor.Current;
            if (!opState.IgnoreTenant /*启用租户*/)
            {
                if (string.IsNullOrEmpty(tenantId))
                {
                    tenantId = currentUser.GetTenantId();
                }
            }

            var cacheKey = $"sharding:tenant:{tenantId}";
            if (!_memoryCache.TryGetValue<string>(cacheKey, out var suffix))
            {
                throw new InvalidOperationException($"未找到租户{tenantId}的路由配置，请检查是否已初始化路由信息");
            }
            return suffix;
        }
        else
        {
            throw new InvalidOperationException("路由分表请实现ITenantId");
        }
    }
}