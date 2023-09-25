namespace Kurisu.EFSharding.Sharding.PaginationConfigurations.SelectPrimaryKeyPagination;

public interface ISelectPrimaryKeyPredicate
{
    public bool ShouldUse(long total, int skip, int tables);
}