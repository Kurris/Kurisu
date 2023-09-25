using Kurisu.DataAccess.Functions.Default;
using Kurisu.DataAccess.Functions.Default.Abstractions;
using Kurisu.Utils.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

// ReSharper disable ClassNeverInstantiated.Global

namespace Kurisu.DataAccess.Internal;

/// <summary>
/// 动态模型缓存工厂
/// </summary>
public class DynamicModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        // return context.GetType().IsAssignableTo(typeof(DefaultAppDbContext<IDbWrite>))
        //     ? (context.GetType(), context.GetExpressionPropertyValue<int?>("UserId"), designTime)
        //     : (context.GetType(), designTime);
        return null; //todo
        // return new ModelCacheKey
    }
}