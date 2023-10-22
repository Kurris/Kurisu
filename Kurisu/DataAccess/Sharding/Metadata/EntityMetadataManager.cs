//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using Kurisu.DataAccess.Sharding.Configurations;
//using Microsoft.EntityFrameworkCore.Metadata;

//namespace Kurisu.DataAccess.Sharding.Metadata;

//public class EntityMetadataManager : IEntityMetadataManager
//{
//    private readonly ShardingOptions _shardingOptions;
//    private readonly ConcurrentDictionary<Type, EntityMetadata> _caches = new();


//    public EntityMetadataManager(ShardingOptions shardingOptions)
//    {
//        _shardingOptions = shardingOptions;
//    }

//    public bool Add(EntityMetadata metadata)
//    {
//        return _caches.TryAdd(metadata.EntityType, metadata);
//    }

//    public bool IsShardingTable(Type type)
//    {
//        throw new NotImplementedException();
//    }

//    public bool IsShardingDatabase(Type type)
//    {
//        throw new NotImplementedException();
//    }

//    public bool IsSharding(Type type)
//    {
//        throw new NotImplementedException();
//    }

//    public EntityMetadata TryGet(Type type)
//    {
//        return !_caches.TryGetValue(type, out var entityMetadata) ? null : entityMetadata;
//    }

//    public List<Type> Types => _caches.Keys.ToList();

//    public bool TryInitModel(IEntityType entityType)
//    {
//        if (_caches.TryGetValue(entityType.ClrType, out var metadata))
//        {
//            metadata.SetEntityModel(entityType);
//            return true;
//        }

//        return false;
//    }
//}