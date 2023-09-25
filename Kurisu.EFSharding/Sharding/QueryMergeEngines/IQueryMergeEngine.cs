namespace Kurisu.EFSharding.Sharding.QueryMergeEngines;


public interface IQueryMergeEngine
{
    void ParseAndRewrite(IQueryable queryable);
}