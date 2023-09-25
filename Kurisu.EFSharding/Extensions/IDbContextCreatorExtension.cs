using Kurisu.EFSharding.Core.DbContextCreator;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.Extensions;

public static class DbContextCreatorExtension
{
    public static DbContext CreateDbContext(this IDbContextCreator dbContextCreator, DbContext mainDbContext, DbContextOptions dbContextOptions,
        IRouteTail routeTail)
    {
        return dbContextCreator.CreateDbContext(mainDbContext, new ShardingDbContextOptions(dbContextOptions, routeTail));
    }
}