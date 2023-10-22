//using System;
//using System.Collections.Generic;
//using Microsoft.EntityFrameworkCore.Metadata;

//namespace Kurisu.DataAccess.Sharding.Metadata;

///// <summary>
///// 实体sharding元数据管理
///// </summary>
//public interface IEntityMetadataManager
//{
//    /// <summary>
//    /// 添加元数据
//    /// </summary>
//    /// <param name="metadata"></param>
//    /// <returns></returns>
//    bool Add(EntityMetadata metadata);

//    /// <summary>
//    /// 是否分表
//    /// </summary>
//    /// <param name="type"></param>
//    /// <returns></returns>
//    bool IsShardingTable(Type type);

//    /// <summary>
//    /// 是否分库
//    /// </summary>
//    /// <param name="type"></param>
//    /// <returns></returns>
//    bool IsShardingDatabase(Type type);

//    /// <summary>
//    /// 是否是分片对象(包括分表或者分库)
//    /// </summary>
//    /// <param name="type"></param>
//    /// <returns></returns>
//    bool IsSharding(Type type);

//    /// <summary>
//    /// 尝试获取
//    /// </summary>
//    /// <param name="type"></param>
//    /// <returns></returns>
//    EntityMetadata TryGet(Type type);

//    List<Type> Types { get; }

//    bool TryInitModel(IEntityType entityType);
//}