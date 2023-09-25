using Kurisu.EFSharding.Core.UnionAllMergeShardingProviders.Abstractions;

namespace Kurisu.EFSharding.Core.UnionAllMergeShardingProviders;

public class UnionAllMergeScope:IDisposable
{
    public IUnionAllMergeAccessor UnionAllMergeAccessor { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="unionAllMergeAccessor"></param>
    public UnionAllMergeScope(IUnionAllMergeAccessor unionAllMergeAccessor)
    {
        UnionAllMergeAccessor = unionAllMergeAccessor;
    }
    public void Dispose()
    {
        UnionAllMergeAccessor.SqlSupportContext = null;
    }
}