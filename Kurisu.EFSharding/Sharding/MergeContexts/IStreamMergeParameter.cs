namespace Kurisu.EFSharding.Sharding.MergeContexts;

internal interface IStreamMergeParameter
{
    IParseResult GetParseResult();

    IRewriteResult GetRewriteResult();
    IOptimizeResult GetOptimizeResult();
}