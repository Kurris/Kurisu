namespace Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions.InMemoryMerge;

/// <summary>
/// 确认结果的合并
/// </summary>
/// <typeparam name="T">返回的确认结果类型</typeparam>
internal interface IEnsureMergeResult<T>
{
    /// <summary>
    /// 合并结果
    /// </summary>
    /// <returns></returns>
    T MergeResult();

    /// <summary>
    /// 合并结果
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<T> MergeResultAsync(CancellationToken cancellationToken = new CancellationToken());
}