using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccess.Functions.Default.Abstractions;

/// <summary>
/// DbContext保存时触发默认值生成处理器
/// </summary>
public interface IDefaultValuesOnSaveChangesResolver
{
    /// <summary>
    /// 数据保存时
    /// </summary>
    /// <param name="dbContext"></param>
    void OnSaveChanges(DbContext dbContext);
}