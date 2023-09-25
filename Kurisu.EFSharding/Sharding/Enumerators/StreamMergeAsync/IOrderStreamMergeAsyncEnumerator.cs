namespace Kurisu.EFSharding.Sharding.Enumerators.StreamMergeAsync;

internal interface IOrderStreamMergeAsyncEnumerator<T>:IStreamMergeAsyncEnumerator<T>, IComparable<IOrderStreamMergeAsyncEnumerator<T>>
{
    List<IComparable> GetCompares();
}