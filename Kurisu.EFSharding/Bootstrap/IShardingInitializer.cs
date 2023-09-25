namespace Kurisu.EFSharding.Bootstrap;

/// <summary>
/// 初始化器
/// </summary>
internal interface IShardingInitializer
{
    /// <summary>
    /// 初始化
    /// </summary>
    void Initialize();
}