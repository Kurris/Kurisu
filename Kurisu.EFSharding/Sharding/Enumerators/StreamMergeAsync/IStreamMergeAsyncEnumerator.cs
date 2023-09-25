namespace Kurisu.EFSharding.Sharding.Enumerators.StreamMergeAsync;

internal interface IStreamMergeAsyncEnumerator<T>:IAsyncEnumerator<T>,IEnumerator<T>
{
    bool SkipFirst();
    bool HasElement();
    T ReallyCurrent { get; }
    T GetCurrent();

}