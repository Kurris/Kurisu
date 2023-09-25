using Kurisu.EFSharding.Core.Metadata.Model;

namespace Kurisu.EFSharding.Core.Metadata;

public interface IMetadataInitializer
{
    void Initialize(BaseShardingMetadata metadata);
}