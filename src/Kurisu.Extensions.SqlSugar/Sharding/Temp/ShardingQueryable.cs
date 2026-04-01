//using System.Linq.Expressions;
//using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
//using Kurisu.Extensions.SqlSugar.Extensions;
//using SqlSugar;

//namespace Kurisu.Extensions.SqlSugar.Sharding;

//public class ShardingQueryable<T> where T : SugarEntity, IShardingRoute, new()
//{
//    private readonly IDbContext _dbContext;
//    private readonly List<Expression<Func<T, bool>>> _whereCollection = new List<Expression<Func<T, bool>>>();
//    private Expression<Func<T, object>> _orderBy;
//    private readonly IShardingRoute _shardingRoute;

//    public ShardingQueryable(IDbContext dbContext)
//    {
//        _dbContext = dbContext;
//        _shardingRoute = new T();
//    }

//    public ShardingQueryable<T> Where(Expression<Func<T, bool>> expression)
//    {
//        _whereCollection.Add(expression);
//        return this;
//    }


//    public ShardingQueryable<T> OrderBy(Expression<Func<T, object>> expression)
//    {
//        if (_orderBy != null)
//        {
//            throw new InvalidOperationException("已经进行数据排序");
//        }
//        _orderBy = expression;
//        return this;
//    }

//    public async Task<List<T>> ToListAsync()
//    {
//        var client = _dbContext.AsSqlSugarDbContext().GetClient();
//        var tableNames = await GetExistsTableNamesAsync(client);

//        var queries = tableNames.Select(table =>
//          {
//              var q = _dbContext.Queryable<T>().AS(table);

//              foreach (var item in _whereCollection)
//              {
//                  q = q.Where(item);
//              }

//              return q.OrderBy(_orderBy);
//          }).ToList();

//        return await client.UnionAll(queries).OrderBy(_orderBy).ToListAsync();
//    }

//    //public async Task<List<T>> ToPageAsync(int pageIndex, int pageSize)
//    //{
//    //    var shardingId = _shardingRoute.GetShardingId();
//    //    var tableName = await _dbContext.Queryable<ShardingRouteTable>().Where(x => x.TanantId == shardingId).Select(x => x.TableSuffix).FirstAsync();
//    //    return await GetShardingResultAsync(_dbContext, tableNames, pageIndex, pageSize);
//    //}


//    //private async Task<Pagination<T>> GetShardingResultAsync(IDbContext dbContext, List<string> tableNames, int pageIndex, int pageSize)
//    //{
//    //    if (pageIndex < 1) pageIndex = 1;
//    //    var offset = (pageIndex - 1) * pageSize;
//    //    var limit = pageSize;

//    //    var fetchPerShard = offset + limit;

//    //    var db = dbContext.AsSqlSugarDbContext().GetClient();

//    //    if (tableNames == null || tableNames.Count == 0)
//    //        return Pagination.Empty<T>(pageIndex, pageSize);

//    //    //第一次查询：用 UNION ALL 找边界时间
//    //    var queries = tableNames.Select(table =>
//    //    {
//    //        var q = dbContext.Queryable<T>().AS(table);

//    //        foreach (var w in _whereCollection)
//    //        {
//    //            q = q.Where(w);
//    //        }

//    //        return q.OrderBy(_orderBy).Take(fetchPerShard); // 每张表最多取 offset+limit 条
//    //    }
//    //    ).ToList();

//    //    var routeKey = _shardingRoute.GetShardingId();

//    //    return await db.UnionAll(queries)
//    //        .OrderBy(_orderBy)
//    //        .ToPageListAsync(new PageQuery(pageIndex, pageSize));
//    //}


//    //private async Task<List<string>> GetExistsTableNamesAsync(ISqlSugarClient client)
//    //{
//    //    var tableName = client.EntityMaintenance.GetTableName(typeof(T));
//    //    return client.DbMaintenance.GetTableInfoList()
//    //          .Where(x => x.DbObjectType == DbObjectType.Table)
//    //          .Where(x => x.Name.StartsWith($"{tableName}_"))
//    //          .Select(x => x.Name)
//    //          .ToList();
//    //}
//}
