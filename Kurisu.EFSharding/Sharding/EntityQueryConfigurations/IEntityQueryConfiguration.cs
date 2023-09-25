namespace Kurisu.EFSharding.Sharding.EntityQueryConfigurations;

public interface IEntityQueryConfiguration<TEntity> where TEntity:class
{
    void Configure(EntityQueryBuilder<TEntity> builder);
}