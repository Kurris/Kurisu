namespace Kurisu.AspNetCore.Abstractions.DataAccess;

/// <summary>
/// 数据源管理器
/// </summary>
// IDatasourceManager：数据源管理器接口
public interface IDatasourceManager : ITransactionManager
{
    object GetCurrentClient();
}