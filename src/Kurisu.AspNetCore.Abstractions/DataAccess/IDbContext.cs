using Kurisu.AspNetCore.Abstractions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.DataAccess;

/// <summary>
/// 只读数据库上下文（当前原始文件中没有查询方法，保留接口以便未来扩展）
/// </summary>
[SkipScan]
public interface IReadDbContext
{
    // 读取相关方法（Query / QueryAsync 等）可在未来添加到此处。
}

/// <summary>
/// 写入数据库上下文（包含 Insert / Update / Delete）
/// </summary>
[SkipScan]
public interface IWriteDbContext
{
    Task<long> InsertReturnIdentityAsync<T>(T obj, CancellationToken cancellationToken = default) where T : class, new();

    long InsertReturnIdentity<T>(T obj) where T : class, new();

    Task<int> InsertAsync<T>(T obj, CancellationToken cancellationToken = default) where T : class, new();
    int Insert<T>(T obj) where T : class, new();

    Task<int> InsertAsync<T>(List<T> objs, CancellationToken cancellationToken = default) where T : class, new();
    bool Insert<T>(List<T> objs) where T : class, new();

    Task<int> DeleteAsync<T>(T obj, bool isReally = false, CancellationToken cancellationToken = default) where T : class, new();
    int Delete<T>(T obj, bool isReally = false) where T : class, new();

    Task<int> DeleteAsync<T>(List<T> objs, bool isReally = false, CancellationToken cancellationToken = default) where T : class, new();
    int Delete<T>(List<T> objs, bool isReally = false) where T : class, new();

    Task<int> UpdateAsync<T>(T obj, CancellationToken cancellationToken = default) where T : class, new();

    Task<int> UpdateAsync<T>(List<T> objs, CancellationToken cancellationToken = default) where T : class, new();

    int Update<T>(T obj) where T : class, new();

    int Update<T>(List<T> objs) where T : class, new();
}

/// <summary>
/// 数据库上下文（组合接口，向后兼容）
/// </summary>
[SkipScan]
public interface IDbContext : IReadDbContext, IWriteDbContext
{
    /// <summary>
    /// 获取查询设置
    /// </summary>
    /// <returns></returns>
    ScopeQuerySetting GetScopeQuerySetting();
}