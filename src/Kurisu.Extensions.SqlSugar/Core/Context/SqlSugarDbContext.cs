using Kurisu.AspNetCore.Abstractions.DataAccess.Contract;
using Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Core.Context;

public class SqlSugarDbContext : SpecificQueryDbContext
{
    public SqlSugarDbContext(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }


    #region insert

    public override Task<bool> InsertAsync<T>(T obj, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Client.Insertable(obj).ExecuteCommandIdentityIntoEntityAsync();
    }

    public override async Task<bool> InsertAsync<T>(List<T> objs, CancellationToken cancellationToken)
    {
        if (objs.Count == 0)
            return true;

        cancellationToken.ThrowIfCancellationRequested();
        return await Client.Insertable(objs).ExecuteCommandAsync() > 0;
    }

    public override bool Insert<T>(T obj)
    {
        return Client.Insertable(obj).ExecuteCommandIdentityIntoEntity();
    }

    public override bool Insert<T>(List<T> objs)
    {
        if (objs.Count == 0)
            return true;

        return Client.Insertable(objs).ExecuteCommand() > 0;
    }

    #endregion


    #region delete

    public override Task<int> DeleteAsync<T>(T obj, bool isReally, CancellationToken cancellationToken)
    {
        return DeleteAsync([obj], isReally, cancellationToken);
    }

    public override Task<int> DeleteAsync<T>(List<T> objs, bool isReally, CancellationToken cancellationToken)
    {
        if (objs.Count == 0)
            return Task.FromResult(0);

        if (typeof(T).IsAssignableTo(typeof(ISoftDeleted)))
        {
            if (isReally)
            {
                return Client.Deleteable(objs).ExecuteCommandAsync(cancellationToken);
            }

            MarkAsDeleted(objs);

            return Client.Updateable(objs)
                .UpdateColumns(nameof(ISoftDeleted.IsDeleted))
                .ExecuteCommandAsync(cancellationToken);
        }

        return Client.Deleteable(objs).ExecuteCommandAsync(cancellationToken);
    }

    public override int Delete<T>(T obj, bool isReally)
    {
        return Delete([obj], isReally);
    }

    public override int Delete<T>(List<T> objs, bool isReally)
    {
        if (objs.Count == 0)
            return 0;

        if (typeof(T).IsAssignableTo(typeof(ISoftDeleted)))
        {
            if (isReally)
            {
                return Client.Deleteable(objs).ExecuteCommand();
            }

            MarkAsDeleted(objs);

            return Client.Updateable(objs)
                .UpdateColumns(nameof(ISoftDeleted.IsDeleted))
                .ExecuteCommand();
        }

        return Client.Deleteable(objs).ExecuteCommand();
    }

    public IDeleteable<T> Deleteable<T>(T obj) where T : class, IEntity, new()
    {
        return Client.Deleteable(obj);
    }

    public IDeleteable<T> Deleteable<T>(List<T> list) where T : class, IEntity, new()
    {
        return Client.Deleteable(list);
    }

    #endregion


    #region update

    public override Task<int> UpdateAsync<T>(T obj, CancellationToken cancellationToken)
    {
        return UpdateAsync(obj, null, cancellationToken);
    }

    public override Task<int> UpdateAsync<T>(T obj, string[] updateColumns, CancellationToken cancellationToken)
    {
        return UpdateAsync([obj], updateColumns, cancellationToken);
    }


    public override Task<int> UpdateAsync<T>(List<T> objs, CancellationToken cancellationToken)
    {
        return UpdateAsync(objs, null, cancellationToken);
    }

    public override Task<int> UpdateAsync<T>(List<T> objs, string[] updateColumns, CancellationToken cancellationToken)
    {
        if (objs.Count == 0)
            return Task.FromResult(0);

        return Client.Updateable(objs)
            .UpdateColumnsIF(updateColumns is { Length: > 0 }, updateColumns)
            .ExecuteCommandAsync(cancellationToken);
    }


    public override int Update<T>(T obj)
    {
        return Update(obj, null);
    }

    public override int Update<T>(T obj, string[] updateColumns)
    {
        return Update([obj], updateColumns);
    }

    public override int Update<T>(List<T> objs)
    {
        return Update(objs, null);
    }

    public override int Update<T>(List<T> objs, string[] updateColumns)
    {
        if (objs.Count == 0)
            return 0;

        return Client.Updateable(objs)
            .UpdateColumnsIF(updateColumns is { Length: > 0 }, updateColumns)
            .ExecuteCommand();
    }

    #endregion

    protected static void MarkAsDeleted<T>(List<T> entities) where T : class
    {
        foreach (var entity in entities)
        {
            ((ISoftDeleted)entity).IsDeleted = true;
        }
    }

}
