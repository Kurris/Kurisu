using Kurisu.AspNetCore.Abstractions.DataAccess.Contract;

namespace Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;

public abstract class WriteAbstractDbContext<TOperationClient> : ReadAbstractDbContext<TOperationClient>, IWriteDbContext where TOperationClient : class
{
    protected WriteAbstractDbContext(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public abstract bool Insert<T>(T obj) where T : class, IEntity, new();

    public abstract bool Insert<T>(List<T> objs) where T : class, IEntity, new();

    public abstract Task<bool> InsertAsync<T>(T obj, CancellationToken cancellationToken) where T : class, IEntity, new();

    public abstract Task<bool> InsertAsync<T>(List<T> objs, CancellationToken cancellationToken) where T : class, IEntity, new();

    public abstract int Delete<T>(T obj, bool isReally) where T : class, IEntity, new();

    public abstract int Delete<T>(List<T> objs, bool isReally) where T : class, IEntity, new();

    public abstract Task<int> DeleteAsync<T>(T obj, bool isReally, CancellationToken cancellationToken) where T : class, IEntity, new();

    public abstract Task<int> DeleteAsync<T>(List<T> objs, bool isReally, CancellationToken cancellationToken) where T : class, IEntity, new();

    public abstract int Update<T>(T obj) where T : class, IEntity, new();

    public abstract int Update<T>(T obj, string[] updateColumns) where T : class, IEntity, new();

    public abstract int Update<T>(List<T> objs) where T : class, IEntity, new();

    public abstract int Update<T>(List<T> objs, string[] updateColumns) where T : class, IEntity, new();

    public abstract Task<int> UpdateAsync<T>(T obj, CancellationToken cancellationToken) where T : class, IEntity, new();

    public abstract Task<int> UpdateAsync<T>(T obj, string[] updateColumns, CancellationToken cancellationToken) where T : class, IEntity, new();

    public abstract Task<int> UpdateAsync<T>(List<T> objs, CancellationToken cancellationToken) where T : class, IEntity, new();

    public abstract Task<int> UpdateAsync<T>(List<T> objs, string[] updateColumns, CancellationToken cancellationToken) where T : class, IEntity, new();
}