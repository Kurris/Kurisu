namespace Kurisu.EFSharding.Sharding.MergeContexts;

public interface IRewriteResult
{
    /// <summary>
    /// 最原始的表达式
    /// </summary>
    /// <returns></returns>
    IQueryable GetCombineQueryable();
    /// <summary>
    /// 被重写后的表达式
    /// </summary>
    /// <returns></returns>
    IQueryable GetRewriteQueryable();
}