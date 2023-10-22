//using System;
//using Kurisu.DataAccess.Sharding.Configurations;
//using Kurisu.DataAccess.Sharding.DependencyInjection;
//using Kurisu.DataAccess.Sharding.Metadata;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;

//namespace Kurisu.DataAccess.Sharding;

///// <summary>
///// 处理sharding的上下文
///// </summary>
///// <typeparam name="TDbContext"></typeparam>
//public sealed class ShardingContext<TDbContext> : IShardingContext<TDbContext> where TDbContext : DbContext, IShardingDbContext
//{
//    private readonly IServiceProvider _shardingServiceProvider;

//    private ShardingContext(Action<IServiceCollection> configure)
//    {
//        IServiceCollection serviceCollection = new ServiceCollection();
//        configure(serviceCollection);
//        _shardingServiceProvider = serviceCollection.BuildServiceProvider();
//    }

//    public static ShardingContext<TDbContext> Initialize(Action<IServiceCollection> configure)
//    {
//        return new ShardingContext<TDbContext>(configure);
//    }


//    public Type DbContextType => typeof(TDbContext);

//    public IModelCacheLockerProvider GetModelCacheLockerProvider()
//    {
//        throw new NotImplementedException();
//    }

//    public IShardingProvider GetShardingProvider()
//    {
//        throw new NotImplementedException();
//    }

//    public ShardingOptions Options => _shardingServiceProvider.GetService<ShardingOptions>();

//    public IEntityMetadataManager GetEntityMetadataManager()
//    {
//        throw new NotImplementedException();
//    }

//    public void GetOrCreateShardingRuntimeModel(DbContext dbContext)
//    {
//        throw new NotImplementedException();
//    }


//    public object GetService(Type serviceType)
//    {
//        throw new NotImplementedException();
//    }

//    public T1 GetService<T1>()
//    {
//        throw new NotImplementedException();
//    }

//    public object GetRequiredService(Type serviceType)
//    {
//        throw new NotImplementedException();
//    }

//    public T1 GetRequiredService<T1>()
//    {
//        throw new NotImplementedException();
//    }
//}