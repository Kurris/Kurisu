using Kurisu.AspNetCore.Abstractions.DataAccess.Contract;
using Kurisu.AspNetCore.Abstractions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;

[SkipScan]
public interface IWriteDbContext
{
    bool Insert<T>(T obj) where T : class, IEntity, new();

    bool Insert<T>(List<T> objs) where T : class, IEntity, new();

    Task<bool> InsertAsync<T>(T obj, CancellationToken cancellationToken = default) where T : class, IEntity, new();

    Task<bool> InsertAsync<T>(List<T> objs, CancellationToken cancellationToken = default) where T : class, IEntity, new();

    int Delete<T>(T obj, bool isReally = false) where T : class, IEntity, new();

    int Delete<T>(List<T> objs, bool isReally = false) where T : class, IEntity, new();

    Task<int> DeleteAsync<T>(T obj, bool isReally = false, CancellationToken cancellationToken = default) where T : class, IEntity, new();

    Task<int> DeleteAsync<T>(List<T> objs, bool isReally = false, CancellationToken cancellationToken = default) where T : class, IEntity, new();

    int Update<T>(T obj) where T : class, IEntity, new();

    int Update<T>(T obj, string[] updateColumns) where T : class, IEntity, new();

    int Update<T>(List<T> objs) where T : class, IEntity, new();

    Task<int> UpdateAsync<T>(T obj, CancellationToken cancellationToken = default) where T : class, IEntity, new();

    Task<int> UpdateAsync<T>(T obj, string[] updateColumns, CancellationToken cancellationToken = default) where T : class, IEntity, new();

    Task<int> UpdateAsync<T>(List<T> objs, CancellationToken cancellationToken = default) where T : class, IEntity, new();
}