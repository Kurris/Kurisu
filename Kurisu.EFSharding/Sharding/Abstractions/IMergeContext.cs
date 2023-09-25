namespace Kurisu.EFSharding.Sharding.Abstractions;

public interface IMergeContext
{
    IQueryable GetCombineQueryable();
    IQueryable GetRewriteQueryable();
    int? GetSkip();
    int? GetTake();
}