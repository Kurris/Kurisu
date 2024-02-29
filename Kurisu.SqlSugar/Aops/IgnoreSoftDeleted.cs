//using Kurisu.Core.DataAccess.Entity;
//using Kurisu.Core.Proxy.Abstractions;
//using Microsoft.AspNetCore.Http;

//namespace Kurisu.SqlSugar.Aops;

///// <summary>
///// 忽略软删除
///// </summary>
//public class IgnoreSoftDeleted : BaseSqlSugarAop
//{
//    public IgnoreSoftDeleted(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
//    {
//    }

//    protected override async Task InterceptAsync(IProxyInvocation invocation, Func<IProxyInvocation, Task> proceed)
//    {
//        var db = Db;
//        try
//        {
//            db.QueryFilter.ClearAndBackup<ISoftDeleted>();
//            await proceed.Invoke(invocation);
//        }
//        finally
//        {
//            db.QueryFilter.Restore();
//        }
//    }

//    protected override async Task<TResult> InterceptAsync<TResult>(IProxyInvocation invocation, Func<IProxyInvocation, Task<TResult>> proceed)
//    {
//        var db = Db;
//        try
//        {
//            db.QueryFilter.ClearAndBackup<ISoftDeleted>();
//            return await proceed.Invoke(invocation);
//        }
//        finally
//        {
//            db.QueryFilter.Restore();
//        }
//    }
//}
