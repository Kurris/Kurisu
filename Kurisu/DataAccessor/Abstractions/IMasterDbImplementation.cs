namespace Kurisu.DataAccessor.Abstractions
{
    /// <summary>
    /// 主库接口
    /// </summary>
    public interface IMasterDbImplementation : IDbCommonOperation, IReadOperation, IWriteOperation
    {
    }
}