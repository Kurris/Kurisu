//using System;
//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations.Schema;
//using System.Linq;
//using System.Reflection;
//using Kurisu.Authentication.Abstractions;
//using Kurisu.DataAccess.Entity;
//using Kurisu.DataAccess.Functions.Default.Abstractions;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Options;

//namespace Kurisu.DataAccess.Functions.Default;

///// <summary>
///// AppDbContext 程序默认DbContext
///// </summary>
//public class DefaultAppDbContext<TDbService> : DbContext where TDbService : IBaseDbService
//{
//    private readonly IQueryFilterResolver _queryFilterResolver;
//    private readonly IModelConfigurationSourceResolver _modelConfigurationSourceResolver;

//    public DefaultAppDbContext(DbContextOptions<DefaultAppDbContext<TDbService>> options
//        , IOptions<KurisuDataAccessorSettingBuilder> builderOptions
//        , IQueryFilterResolver queryFilterResolver
//        , IModelConfigurationSourceResolver modelConfigurationSourceResolver
//        , ICurrentUserInfoResolver currentUserInfoResolver) : base(options)
//    {
//        _queryFilterResolver = queryFilterResolver;
//        _modelConfigurationSourceResolver = modelConfigurationSourceResolver;

//        UserId = currentUserInfoResolver.GetSubjectId<int?>();
//    }

//    /// <summary>
//    /// 当前请求用户id
//    /// </summary>
//    public int? UserId { get; }

//    /// <summary>
//    /// 扫描TableAttribute|IBaseEntity的实体类型
//    /// </summary>
//    protected readonly IReadOnlyList<Type> EntityTypes = App.ActiveTypes.Where(x => !x.IsAbstract).Where(x => x.IsDefined(typeof(TableAttribute)) || x.IsAssignableTo(typeof(IBaseEntity))).ToList();

//    protected override void OnModelCreating(ModelBuilder modelBuilder)
//    {
//        foreach (var entityType in EntityTypes)
//        {
//            //避免导航属性重复加载
//            if (modelBuilder.Model.GetEntityTypes().Any(x => x.Name.Equals(entityType.FullName, StringComparison.OrdinalIgnoreCase)))
//                continue;

//            var builder = modelBuilder.Entity(entityType);

//            if (entityType.IsDefined(typeof(EnableSplitTableAttribute), false))
//            {
//                var suffix = (UserId ?? 1).GetHashCode() % 3;
//                builder.ToTable(entityType.GetCustomAttribute<TableAttribute>()!.Name + "_" + suffix);
//            }

//            //查询过滤
//            _queryFilterResolver.HandleQueryFilter(this, entityType, builder);
//        }

//        //加载模型配置
//        var assembly = _modelConfigurationSourceResolver.GetAssembly();
//        if (assembly != null)
//        {
//            //modelBuilder.ApplyConfiguration();
//            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
//        }

//        base.OnModelCreating(modelBuilder);
//    }
//}

///// <summary>
///// 程序默认数据库上下文
///// </summary>
//public class DefaultAppDbContext : DefaultAppDbContext<IDbWrite>
//{
//    public DefaultAppDbContext(DbContextOptions<DefaultAppDbContext<IDbWrite>> options
//        , IOptions<KurisuDataAccessorSettingBuilder> builderOptions
//        , IQueryFilterResolver queryFilterResolver
//        , IModelConfigurationSourceResolver modelConfigurationSourceResolver
//        , ICurrentUserInfoResolver currentUserInfoResolver)
//        : base(options, builderOptions, queryFilterResolver, modelConfigurationSourceResolver, currentUserInfoResolver)
//    {
//    }
//}