using Kurisu.EFSharding.Core.RuntimeContexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.EFSharding.EFCores.EFCore6x;

public class ShardingOptionsExtension : IDbContextOptionsExtension
{
    public IShardingRuntimeContext ShardingRuntimeContext { get; }

    public ShardingOptionsExtension(IShardingRuntimeContext shardingRuntimeContext)
    {
        ShardingRuntimeContext = shardingRuntimeContext;
    }

    public void ApplyServices(IServiceCollection services)
    {
        services.AddSingleton<IShardingRuntimeContext>(sp => ShardingRuntimeContext);
    }

    public void Validate(IDbContextOptions options)
    {
    }


    public DbContextOptionsExtensionInfo Info => new ShardingOptionsExtensionInfo(this);

    private class ShardingOptionsExtensionInfo : DbContextOptionsExtensionInfo
    {
        private readonly ShardingOptionsExtension _shardingOptionsExtension;

        public ShardingOptionsExtensionInfo(IDbContextOptionsExtension extension) : base(extension)
        {
            _shardingOptionsExtension = (ShardingOptionsExtension) extension;
        }

        public override int GetServiceProviderHashCode() =>
            _shardingOptionsExtension.ShardingRuntimeContext.GetHashCode();

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => true;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
        }

        public override bool IsDatabaseProvider => false;
        public override string LogFragment => "ShardingOptionsExtension";
    }
}