using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.EFSharding.EFCores.EFCore6x;

public class ShardingWrapOptionsExtension : IDbContextOptionsExtension
{
    public void ApplyServices(IServiceCollection services)
    {
    }

    public void Validate(IDbContextOptions options)
    {
    }


    public DbContextOptionsExtensionInfo Info => new ShardingWrapDbContextOptionsExtensionInfo(this);

    private class ShardingWrapDbContextOptionsExtensionInfo : DbContextOptionsExtensionInfo
    {
        public ShardingWrapDbContextOptionsExtensionInfo(IDbContextOptionsExtension extension) : base(extension)
        {
        }

        public override int GetServiceProviderHashCode() => 0;

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => true;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
        }

        public override bool IsDatabaseProvider => false;
        public override string LogFragment => "ShardingWrapOptionsExtension";
    }
}