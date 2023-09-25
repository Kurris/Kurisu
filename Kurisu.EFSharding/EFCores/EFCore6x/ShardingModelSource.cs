using Kurisu.EFSharding.Core.RuntimeContexts;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Caching.Memory;

namespace Kurisu.EFSharding.EFCores.EFCore6x;

public class ShardingModelSource : ModelSource
{
    private readonly IShardingRuntimeContext _shardingRuntimeContext;

    public ShardingModelSource(ModelSourceDependencies dependencies, IShardingRuntimeContext shardingRuntimeContext) : base(dependencies)
    {
        _shardingRuntimeContext = shardingRuntimeContext;
    }

    /// <summary>
    ///     Gets the model to be used.
    /// </summary>
    /// <param name="context">The context the model is being produced for.</param>
    /// <param name="modelCreationDependencies">The dependencies object used during the creation of the model.</param>
    /// <param name="designTime">Whether the model should contain design-time configuration.</param>
    /// <returns>The model to be used.</returns>
    public override IModel GetModel(
        DbContext context,
        ModelCreationDependencies modelCreationDependencies,
        bool designTime)
    {
        CacheItemPriority? setPriority = null;
        if (context is IShardingDbContext shardingTableDbContext)
        {
            // if (context is IShardingTableDbContext shardingTableDbContext)
            // {
            switch (shardingTableDbContext.RouteTail)
            {
                case null:
                    throw new ShardingCoreInvalidOperationException("db context model is inited before RouteTail set value");
                case INoCacheRouteTail:
                {
                    var noCacheModel = CreateModel(context, modelCreationDependencies.ConventionSetBuilder, modelCreationDependencies.ModelDependencies);
                    noCacheModel = modelCreationDependencies.ModelRuntimeInitializer.Initialize(noCacheModel, designTime, modelCreationDependencies.ValidationLogger);
                    return noCacheModel;
                }
                case ISingleQueryRouteTail singleQueryRouteTail when singleQueryRouteTail.IsShardingTableQuery():
                    setPriority = CacheItemPriority.Normal;
                    break;
            }
        }

        var cache = Dependencies.MemoryCache;
        var cacheKey = Dependencies.ModelCacheKeyFactory.Create(context, designTime);
        if (!cache.TryGetValue(cacheKey, out IModel model))
        {
            var modelCacheLockerProvider = _shardingRuntimeContext.GetModelCacheLockerProvider();

            var priority = setPriority ?? modelCacheLockerProvider.GetCacheItemPriority();
            var size = modelCacheLockerProvider.GetCacheEntrySize();
            var waitSeconds = modelCacheLockerProvider.GetCacheModelLockObjectSeconds();
            var cacheLockObject = modelCacheLockerProvider.GetCacheLockObject(cacheKey);
            // Make sure OnModelCreating really only gets called once, since it may not be thread safe.
            var acquire = Monitor.TryEnter(cacheLockObject, TimeSpan.FromSeconds(waitSeconds));
            if (!acquire)
            {
                throw new ShardingCoreInvalidOperationException("cache model timeout");
            }

            try
            {
                if (!cache.TryGetValue(cacheKey, out model))
                {
                    model = CreateModel(
                        context, modelCreationDependencies.ConventionSetBuilder, modelCreationDependencies.ModelDependencies);

                    model = modelCreationDependencies.ModelRuntimeInitializer.Initialize(
                        model, designTime, modelCreationDependencies.ValidationLogger);

                    model = cache.Set(cacheKey, model, new MemoryCacheEntryOptions {Size = size, Priority = priority});
                }
            }
            finally
            {
                Monitor.Exit(cacheLockObject);
            }
        }

        return model;
    }
}