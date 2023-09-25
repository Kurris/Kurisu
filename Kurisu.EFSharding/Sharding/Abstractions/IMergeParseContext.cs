namespace Kurisu.EFSharding.Sharding.Abstractions;

internal interface IMergeParseContext
{
    int? GetSkip();
    int? GetTake();
}