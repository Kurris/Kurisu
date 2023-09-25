namespace Kurisu.EFSharding.Sharding.Enumerators.StreamMergeAsync;

internal interface IInMemoryStreamMergeAsyncEnumerator 
{
    int GetReallyCount();
}
internal interface IInMemoryStreamMergeAsyncEnumerator<T> : IStreamMergeAsyncEnumerator<T>, IInMemoryStreamMergeAsyncEnumerator
{
}