using Kurisu.AspNetCore.Abstractions.DataAccess.Contract;
using Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Core.Context;

internal class SqlSugarDbContext : SpecialQueryableDbContext
{
    public SqlSugarDbContext(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }


    #region insert

    public override async Task<bool> InsertAsync<T>(T obj, CancellationToken cancellationToken)
    {
        return await Client.Insertable(obj).ExecuteCommandIdentityIntoEntityAsync();
    }

    public override async Task<bool> InsertAsync<T>(List<T> objs, CancellationToken cancellationToken)
    {
        return await Client.Insertable(objs.ToArray()).ExecuteCommandIdentityIntoEntityAsync();
    }

    public override bool Insert<T>(T obj)
    {
        return Client.Insertable(obj).ExecuteCommandIdentityIntoEntity();
    }

    public override bool Insert<T>(List<T> objs)
    {
        return Client.Insertable(objs.ToArray()).ExecuteCommandIdentityIntoEntity();
    }

    #endregion


    #region delete

    public override async Task<int> DeleteAsync<T>(T obj, bool isReally, CancellationToken cancellationToken)
    {
        if (isReally || !typeof(T).IsAssignableTo(typeof(ISoftDeleted)))
        {
            return await this.Deleteable(obj).ExecuteCommandAsync(cancellationToken);
        }

        obj.SetPropertyValue(nameof(ISoftDeleted.IsDeleted), true);
        return await this.UpdateAsync(obj, cancellationToken);
    }

    public override async Task<int> DeleteAsync<T>(List<T> objs, bool isReally, CancellationToken cancellationToken)
    {
        var list = objs.ToList();

        if (isReally || !typeof(T).IsAssignableTo(typeof(ISoftDeleted)))
        {
            var total = 0;
            foreach (var item in list)
            {
                total += await this.DeleteAsync(item, isReally, cancellationToken);
            }

            return total;
        }

        list.ForEach(x => x.SetPropertyValue(nameof(ISoftDeleted.IsDeleted), true));
        return await this.UpdateAsync(list, cancellationToken);
    }

    public override int Delete<T>(T obj, bool isReally)
    {
        if (isReally || !typeof(T).IsAssignableTo(typeof(ISoftDeleted)))
        {
            return this.Deleteable(obj).ExecuteCommand();
        }

        obj.SetPropertyValue(nameof(ISoftDeleted.IsDeleted), true);
        return this.Update(obj);
    }

    public override int Delete<T>(List<T> objs, bool isReally)
    {
        if (isReally || !typeof(T).IsAssignableTo(typeof(ISoftDeleted)))
        {
            var total = 0;
            foreach (var item in objs)
            {
                total += Delete(item, isReally);
            }

            return total;
        }

        objs.ForEach(x => x.SetPropertyValue(nameof(ISoftDeleted.IsDeleted), true));
        return Update(objs);
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
        return await Client.Updateable(obj).ExecuteCommandAsync(cancellationToken);
    }

    public override async Task<int> UpdateAsync<T>(T obj, string[] updateColumns, CancellationToken cancellationToken)
    {
        return await Client.Updateable(obj).UpdateColumns(updateColumns).ExecuteCommandAsync(cancellationToken);
    }

    public override async Task<int> UpdateAsync<T>(List<T> objs, string[] updateColumns, CancellationToken cancellationToken)
    {
        return await Client.Updateable(objs).UpdateColumns(updateColumns).ExecuteCommandAsync(cancellationToken);
    }

    public override async Task<int> UpdateAsync<T>(List<T> objs, CancellationToken cancellationToken)
    {
        return await Client.Updateable(objs).ExecuteCommandAsync(cancellationToken);
    }

    public override int Update<T>(T obj)
    {
        return Client.Updateable(obj).ExecuteCommand();
    }

    public override int Update<T>(T obj, string[] updateColumns)
    {
        return Client.Updateable(obj).UpdateColumns(updateColumns).ExecuteCommand();
    }

    public override int Update<T>(List<T> objs)
    {
        return Client.Updateable(objs).ExecuteCommand();
    }

    public override int Update<T>(List<T> objs, string[] updateColumns)
    {
        return Client.Updateable(objs).UpdateColumns(updateColumns).ExecuteCommand();
    }

    #endregion
}

public static class DbContextExtensions
{
    public static void SetPropertyValue<T>(this T obj, string propertyName, object value)
    {
        var prop = typeof(T).GetProperty(propertyName);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(obj, value);
        }
    }
}