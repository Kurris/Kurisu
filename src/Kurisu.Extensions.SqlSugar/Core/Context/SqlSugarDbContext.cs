using Kurisu.AspNetCore.Abstractions.DataAccess.Contract;
using Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Core.Context;

public class SqlSugarDbContext : SpecialQueryableDbContext
{
    public SqlSugarDbContext(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }


    #region insert

    public override async Task<bool> InsertAsync<T>(T obj, CancellationToken cancellationToken)
    {
        return await this.InsertAsync(new List<T> { obj }, cancellationToken);
    }

    public override async Task<bool> InsertAsync<T>(List<T> objs, CancellationToken cancellationToken)
    {
        if (objs.Count > 0)
        {
            return (await Client.Insertable(objs).ExecuteCommandAsync()) > 0;
        }

        return true;
    }

    public override bool Insert<T>(T obj)
    {
        return this.Insert(new List<T> { obj });
    }

    public override bool Insert<T>(List<T> objs)
    {
        return Client.Insertable(objs).ExecuteCommandIdentityIntoEntity();
    }

    #endregion


    #region delete

    public override async Task<int> DeleteAsync<T>(T obj, bool isReally, CancellationToken cancellationToken)
    {
        return await DeleteAsync(new List<T> { obj }, isReally, cancellationToken);
    }

    public override async Task<int> DeleteAsync<T>(List<T> objs, bool isReally, CancellationToken cancellationToken)
    {
        if (typeof(T).IsAssignableTo(typeof(ISoftDeleted)))
        {
            if (isReally)
            {
                return await Client.Deleteable(objs).ExecuteCommandAsync(cancellationToken);
            }

            foreach (ISoftDeleted item in objs)
            {
                item.IsDeleted = true;
            }

            return await UpdateAsync(objs, cancellationToken);
        }
        else
        {
            return await Client.Deleteable(objs).ExecuteCommandAsync(cancellationToken);
        }
    }

    public override int Delete<T>(T obj, bool isReally)
    {
        return Delete(new List<T> { obj }, isReally);
    }

    public override int Delete<T>(List<T> objs, bool isReally)
    {
        if (typeof(T).IsAssignableTo(typeof(ISoftDeleted)))
        {
            if (isReally)
            {
                return Client.Deleteable(objs).ExecuteCommand();
            }

            foreach (ISoftDeleted item in objs)
            {
                item.IsDeleted = true;
            }

            return Update(objs);
        }
        else
        {
            return Client.Deleteable(objs).ExecuteCommand();
        }
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

    public override async Task<int> UpdateAsync<T>(T obj, CancellationToken cancellationToken)
    {
        return await UpdateAsync(obj, null, cancellationToken);
    }

    public override async Task<int> UpdateAsync<T>(T obj, string[] updateColumns, CancellationToken cancellationToken)
    {
        return await UpdateAsync(new List<T> { obj }, updateColumns, cancellationToken);
    }


    public override async Task<int> UpdateAsync<T>(List<T> objs, CancellationToken cancellationToken)
    {
        return await UpdateAsync(objs, null, cancellationToken);
    }

    public override async Task<int> UpdateAsync<T>(List<T> objs, string[] updateColumns, CancellationToken cancellationToken)
    {
        if (objs.Count > 0)
        {
            return await Client.Updateable(objs).UpdateColumnsIF(updateColumns != null && updateColumns.Length > 0, updateColumns).ExecuteCommandAsync(cancellationToken);
        }
        return 0;
    }


    public override int Update<T>(T obj)
    {
        return Update(obj, null);
    }

    public override int Update<T>(T obj, string[] updateColumns)
    {
        return Update(new List<T> { obj }, updateColumns);
    }

    public override int Update<T>(List<T> objs)
    {
        return Update(objs, null);
    }

    public override int Update<T>(List<T> objs, string[] updateColumns)
    {
        return Client.Updateable(objs).UpdateColumnsIF(updateColumns != null && updateColumns.Length > 0, updateColumns).ExecuteCommand();
    }

    #endregion
}