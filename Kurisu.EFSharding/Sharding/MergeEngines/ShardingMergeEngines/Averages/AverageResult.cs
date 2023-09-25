namespace Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Averages;

public class AverageResult<T>
{
    public AverageResult(T sum, long count)
    {
        Sum = sum;
        Count = count;
    }

    public T Sum { get; }
    public long Count { get; }

}