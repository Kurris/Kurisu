namespace Kurisu.EFSharding.Sharding.MergeContexts;


public interface IParseResult
{
    PaginationContext GetPaginationContext();
    OrderByContext GetOrderByContext();

    SelectContext GetSelectContext();

    GroupByContext GetGroupByContext();
    bool IsEmunerableQuery();
    string QueryMethodName();
}