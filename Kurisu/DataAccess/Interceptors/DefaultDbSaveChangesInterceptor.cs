//using System.Threading;
//using System.Threading.Tasks;
//using Kurisu.DataAccess.Functions.Default.Abstractions;
//using Microsoft.EntityFrameworkCore.Diagnostics;
//using Microsoft.Extensions.DependencyInjection;

//namespace Kurisu.DataAccess.Interceptors;

///// <summary>
///// 上下文保存拦截器
///// </summary>
//public class DefaultDbSaveChangesInterceptor : SaveChangesInterceptor
//{
//    private readonly IDefaultValuesOnSaveChangesResolver _defaultValuesOnSaveChangesResolver;

//    public DefaultDbSaveChangesInterceptor(IDefaultValuesOnSaveChangesResolver defaultValuesOnSaveChangesResolver)
//    {
//        _defaultValuesOnSaveChangesResolver = defaultValuesOnSaveChangesResolver;
//    }

//    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
//    {
//        _defaultValuesOnSaveChangesResolver.OnSaveChanges(eventData.Context);
//        return base.SavingChanges(eventData, result);
//    }

//    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = new CancellationToken())
//    {
//        _defaultValuesOnSaveChangesResolver.OnSaveChanges(eventData.Context);
//        return base.SavingChangesAsync(eventData, result, cancellationToken);
//    }
//}