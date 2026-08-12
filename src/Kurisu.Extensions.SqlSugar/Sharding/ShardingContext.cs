using Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Kurisu.Extensions.ContextAccessor.Abstractions;
using Kurisu.Extensions.SqlSugar.Context;
using Kurisu.Extensions.SqlSugar.Core.Context;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Extensions.SqlSugar.Sharding;


public class ShardingContext : SqlSugarDbContext
{
    private readonly IContextSnapshotManager<DbOperationState> _contextSnapshotManager;
    private readonly IShardingRouteResolver _resolver;

    public ShardingContext(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _contextSnapshotManager = serviceProvider.GetRequiredService<IContextSnapshotManager<DbOperationState>>();
        _resolver = serviceProvider.GetRequiredService<IShardingRouteResolver>();
    }

    private bool EnableSharding<T>()
    {
        if (ShardingEntityHelper.IsEnabled<T>())
        {
            if (!typeof(T).IsAssignableTo(typeof(ITenantId)))
            {
                return false;
            }

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

    public override Task<bool> InsertAsync<T>(T obj, CancellationToken cancellationToken)
    {
        if (EnableSharding<T>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tableName = GetShardingTableName(obj);
            return Client.Insertable(obj).AS(tableName).ExecuteCommandIdentityIntoEntityAsync();
        }

        return base.InsertAsync(obj, cancellationToken);
    }

    public override bool Insert<T>(List<T> objs)
    {
        if (objs.Count == 0)
            return true;

        if (EnableSharding<T>())
        {
            var dict = GetShardingTableNames(objs);
            foreach (var kv in dict)
            {
                var tableName = kv.Key;
                var list = kv.Value;
                if (Client.Insertable(list).AS(tableName).ExecuteCommand() <= 0)
                {
                    return false;
                }
            }

            return true;
        }

        return base.Insert(objs);
    }


    public override async Task<bool> InsertAsync<T>(List<T> objs, CancellationToken cancellationToken)
    {
        if (objs.Count == 0)
            return true;

        if (EnableSharding<T>())
        {
            var dict = GetShardingTableNames(objs);
            foreach (var kv in dict)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var tableName = kv.Key;
                var list = kv.Value;
                if (await Client.Insertable(list).AS(tableName).ExecuteCommandAsync() <= 0)
                {
                    return false;
                }
            }

            return true;
        }

        return await base.InsertAsync(objs, cancellationToken);
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
                var count = Client.Updateable(list).AS(tableName)
                    .UpdateColumnsIF(updateColumns is { Length: > 0 }, updateColumns)
                    .ExecuteCommand();
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
                    var count = await Client.Updateable(list).AS(tableName)
                        .UpdateColumnsIF(updateColumns is { Length: > 0 }, updateColumns)
                        .ExecuteCommandAsync(cancellationToken);
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
                        MarkAsDeleted(list);
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
                        MarkAsDeleted(list);
                        count = await Client.Updateable(list).AS(tableName)
                            .UpdateColumns(nameof(ISoftDeleted.IsDeleted))
                            .ExecuteCommandAsync(cancellationToken);
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
        if (obj is not ITenantId tenant)
        {
            throw new InvalidOperationException("路由分表请实现ITenantId");
        }

        var tenantId = tenant.TenantId;
        var opState = _contextSnapshotManager.ContextAccessor.Current;
        if (!opState.IgnoreTenant && string.IsNullOrEmpty(tenantId))
        {
            tenantId = ServiceProvider.GetRequiredService<IDbTenantAccessor>().GetTenantId();
        }

        if (string.IsNullOrEmpty(tenantId))
        {
            throw new InvalidOperationException("未能解析分表租户ID，请确认实体 TenantId 或当前用户租户信息已设置");
        }

        return _resolver.GetSuffix(tenantId);
    }
}
