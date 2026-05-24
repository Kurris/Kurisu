namespace Kurisu.AspNetCore.Abstractions.DataAccess;

/// <summary>
/// 定义获取数据权限
/// </summary>
public interface IGetDataPermissions
{
    /// <summary>
    /// 获取数据
    /// </summary>
    /// <returns></returns>
    Dictionary<string, IReadOnlyList<object>> GetData<T>();
}
