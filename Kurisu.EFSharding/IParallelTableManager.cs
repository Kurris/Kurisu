namespace Kurisu.EFSharding;

public interface IParallelTableManager
{
    /// <summary>
    /// 添加平行表
    /// </summary>
    /// <param name="parallelTableGroupNode"></param>
    /// <returns></returns>
    bool AddParallelTable(ParallelTableGroupNode parallelTableGroupNode);

    /// <summary>
    /// 是否是平行表查询
    /// </summary>
    /// <param name="entityTypes"></param>
    /// <returns></returns>
    bool IsParallelTableQuery(IEnumerable<Type> entityTypes);

    /// <summary>
    /// 是否是平行表查询
    /// </summary>
    /// <param name="parallelTableGroupNode"></param>
    /// <returns></returns>
    bool IsParallelTableQuery(ParallelTableGroupNode parallelTableGroupNode);
}