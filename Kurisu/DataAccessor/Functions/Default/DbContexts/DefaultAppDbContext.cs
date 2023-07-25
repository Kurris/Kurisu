using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Entity;
using Kurisu.DataAccessor.Functions.Default.Abstractions;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kurisu.DataAccessor.Functions.Default.DbContexts;

/// <summary>
/// AppDbContext 程序默认DbContext
/// </summary>
public class DefaultAppDbContext<TDbService> : DbContext, IDbContextSoftDeleted where TDbService : IBaseDbService
{
    private readonly IDefaultValuesOnSaveChangesResolver _defaultValuesOnSaveChangesResolver;
    private readonly IQueryFilterResolver _queryFilterResolver;
    private readonly IModelConfigurationSourceResolver _modelConfigurationSourceResolver;


    public DefaultAppDbContext(DbContextOptions<DefaultAppDbContext<TDbService>> options
        , IOptions<KurisuDataAccessorBuilderSetting> builderOptions
        , IDefaultValuesOnSaveChangesResolver defaultValuesOnSaveChangesResolver
        , IQueryFilterResolver queryFilterResolver
        , IModelConfigurationSourceResolver modelConfigurationSourceResolver) : base(options)
    {
        _defaultValuesOnSaveChangesResolver = defaultValuesOnSaveChangesResolver;
        _queryFilterResolver = queryFilterResolver;
        _modelConfigurationSourceResolver = modelConfigurationSourceResolver;

        //true
        IsEnableSoftDeleted = builderOptions.Value.IsEnableSoftDeleted;
    }

    /// <summary>
    /// 是否开启软删除(默认:true)
    /// </summary>
    public bool IsEnableSoftDeleted { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //定义TableAttribute,并且非继承
        var entityTypes = App.ActiveTypes.Where(x => x.IsDefined(typeof(TableAttribute), false)
                                                     && x.BaseType is {IsGenericType: true}
                                                     && x.BaseType.GetGenericTypeDefinition() == typeof(BaseEntity<,>));

        foreach (var entityType in entityTypes)
        {
            //避免导航属性重复加载
            if (modelBuilder.Model.GetEntityTypes().Any(x => x.Name.Equals(entityType.FullName, StringComparison.OrdinalIgnoreCase)))
                continue;

            var builder = modelBuilder.Entity(entityType);

            //查询过滤
            _queryFilterResolver.HandleQueryFilter(this, entityType, builder);
        }

        //加载模型配置
        var assembly = _modelConfigurationSourceResolver.GetSourceAssembly();
        if (assembly != null)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }

        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        GenerateDefaultValues();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new())
    {
        GenerateDefaultValues();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }


    /// <summary>
    /// 生成默认值
    /// </summary>
    // ReSharper disable once VirtualMemberNeverOverridden.Global
    protected virtual void GenerateDefaultValues()
    {
        _defaultValuesOnSaveChangesResolver.OnSaveChanges(this);
    }
}

/// <summary>
/// 程序默认数据库上下文
/// </summary>
public class DefaultAppDbContext : DefaultAppDbContext<IAppMasterDb>
{
    public DefaultAppDbContext(DbContextOptions<DefaultAppDbContext<IAppMasterDb>> options
        , IOptions<KurisuDataAccessorBuilderSetting> builderOptions
        , IDefaultValuesOnSaveChangesResolver defaultValuesOnSaveChangesResolver
        , IQueryFilterResolver queryFilterResolver
        , IModelConfigurationSourceResolver modelConfigurationSourceResolver)
        : base(options, builderOptions, defaultValuesOnSaveChangesResolver, queryFilterResolver, modelConfigurationSourceResolver)
    {
    }
}