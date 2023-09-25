using System.Diagnostics.CodeAnalysis;
using Kurisu.EFSharding.Extensions;
using Kurisu.EFSharding.Extensions.DbContextExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;

namespace Kurisu.EFSharding.EFCores;

[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class ShardingDbSetInitializer : DbSetInitializer
{
    public ShardingDbSetInitializer(IDbSetFinder setFinder, IDbSetSource setSource) : base(setFinder, setSource)
    {
    }

    public override void InitializeSets(DbContext context)
    {
        base.InitializeSets(context);
        if (context.IsShellDbContext())
        {
            context.GetShardingRuntimeContext().GetOrCreateShardingRuntimeModel(context);
        }
    }
}