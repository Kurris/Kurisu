using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.Extensions.SqlSugar.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Extensions.SqlSugar.Sharding;

/// <summary>
/// 默认分表路由解析器（单例），通过 <see cref="IServiceScopeFactory"/> 创建 scope 查询 <see cref="ShardingRouteTable"/> 获取分表后缀
/// </summary>
public class DefaultShardingRouteResolver : IShardingRouteResolver
{
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// ctor
    /// </summary>
    public DefaultShardingRouteResolver(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public string GetSuffix(string tenantId)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
        using var datasourceScope = dbContext.CreateDatasourceScope();

        var suffix = dbContext.Queryable<ShardingRouteTable>()
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.TableSuffix)
            .First();

        if (string.IsNullOrEmpty(suffix))
        {
            throw new InvalidOperationException($"无法为租户 {tenantId} 找到分表后缀");
        }

        return suffix;
    }
}