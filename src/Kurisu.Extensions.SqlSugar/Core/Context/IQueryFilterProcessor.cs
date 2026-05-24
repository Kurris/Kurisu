using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Core.Context;

public interface IQueryFilterProcessor
{
    ISugarQueryable<T> Apply<T>(ISugarQueryable<T> query);
}
