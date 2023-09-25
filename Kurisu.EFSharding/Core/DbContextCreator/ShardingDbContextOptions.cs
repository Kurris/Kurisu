using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.Core.DbContextCreator;

public class ShardingDbContextOptions
{
    public ShardingDbContextOptions(DbContextOptions dbContextOptions, IRouteTail routeTail)
    {
        RouteTail = routeTail;
        DbContextOptions = dbContextOptions;
    }

    public IRouteTail RouteTail { get; }

    public DbContextOptions DbContextOptions { get; }
}