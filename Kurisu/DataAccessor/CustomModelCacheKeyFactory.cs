using Kurisu.Utils.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Kurisu.DataAccessor;

public class CustomModelCacheKeyFactory : ModelCacheKeyFactory
{
    public CustomModelCacheKeyFactory(ModelCacheKeyFactoryDependencies dependencies) : base(dependencies)
    {
    }

    public override object Create(DbContext context)
    {
        var key = context.GetExpressionPropertyValue<int?>("UserId");
        return new CustomModelCacheKey(key, context);
    }
}