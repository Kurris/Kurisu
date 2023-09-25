namespace Kurisu.EFSharding.Sharding.PaginationConfigurations;


public interface IPaginationConfiguration<TEntity> where TEntity : class
{
    void Configure(PaginationBuilder<TEntity> builder);
}