using Kurisu.EFSharding.Core.DbContextCreator;
using Kurisu.EFSharding.Core.DependencyInjection;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding.Abstractions;
using Kurisu.EFSharding.Extensions;
using Kurisu.EFSharding.Extensions.DbContextExtensions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Kurisu.EFSharding.TableCreator;

internal class ShardingTableCreator : IShardingTableCreator
{
    private readonly ILogger<ShardingTableCreator> _logger;
    private readonly IShardingProvider _shardingProvider;
    private readonly IRouteTailFactory _routeTailFactory;
    private readonly IDbContextCreator _dbContextCreator;

    public ShardingTableCreator(IShardingProvider shardingProvider, IRouteTailFactory routeTailFactory, IDbContextCreator dbContextCreator,
        ILogger<ShardingTableCreator> logger)
    {
        _shardingProvider = shardingProvider;
        _routeTailFactory = routeTailFactory;
        _dbContextCreator = dbContextCreator;
        _logger = logger;
    }

    public async Task CreateTableAsync(string dataSourceName, Type shardingEntityType, string tail)
    {
        using var scope = _shardingProvider.CreateScope();
        await using var shellDbContext = _dbContextCreator.GetShellDbContext(scope.ServiceProvider);
        await using var context = ((IShardingDbContext) shellDbContext).GetIndependentWriteDbContext(dataSourceName, _routeTailFactory.Create(tail, false));

        context.RemoveAllExceptTable(shardingEntityType);
        var databaseCreator = (context.GetService<IDatabaseCreator>() as RelationalDatabaseCreator)!;
        try
        {
            await databaseCreator.CreateTablesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Create table error.Entity name is {Name}", shardingEntityType.Name);
            throw new ShardingCoreException($"Create table error :{ex.Message}", ex);
        }
    }
}