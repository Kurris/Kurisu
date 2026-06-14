using System.Data;
using Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.Extensions.ContextAccessor;
using Kurisu.Extensions.ContextAccessor.Abstractions;
using Kurisu.Extensions.SqlSugar.Context;
using Kurisu.Extensions.SqlSugar.Attributes;
using Kurisu.Extensions.SqlSugar.Core.Context;
using Kurisu.Extensions.SqlSugar.Core.Manager;
using Kurisu.Extensions.SqlSugar.Sharding;
using Kurisu.Extensions.SqlSugar.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlSugar;
using DbType = SqlSugar.DbType;

namespace Kurisu.Extensions.SqlSugar;

/// <summary>
/// SqlSugar Ioc 注入
/// </summary>
public static class SqlSugarServiceCollectionExtensions
{
    /// <summary>
    /// 添加SQLSugar
    /// </summary>
    /// <param name="services"></param>
    /// <param name="dbType"></param>
    /// <param name="configDb"></param>
    public static SqlSugarServiceBuilder AddSqlSugar(this IServiceCollection services, DbType dbType, Action<IServiceProvider, ISqlSugarClient> configDb = null)
    {
        services.TryAddSingleton<IDbConnectionRegistry>(sp =>
        {
            var dbOptions = sp.GetService<IOptions<DbOptions>>().Value;
            var registry = new SqlSugarConnectionRegistry();
            registry.Register(nameof(dbOptions.DefaultConnectionString), dbOptions.DefaultConnectionString);
            registry.Register(dbOptions.AdditionalConnectionStrings);
            return registry;
        });

        services.AddContextAccessor<DbOperationState>().WithSnapshot();
        services.AddContextAccessor<NamesDbConnectionStringStack>();

        services.TryAddSingleton<IDbConnectionStringManager, SqlSugarConnectionStringManager>();
        // Transient：
        services.TryAddTransient<IDatasourceManager<ISqlSugarClient>, SqlSugarDatasourceManager>();
        services.TryAddTransient<IDatasourceManager>(sp => sp.GetRequiredService<IDatasourceManager<ISqlSugarClient>>());

        services.TryAddSingleton<DefaultSqlSugarConfigHandler>();
        services.TryAddScoped<IQueryFilterProcessor, DefaultQueryFilterProcessor>();
        services.TryAddSingleton<IDbAuditAccessor, NullDbAuditAccessor>();
        services.TryAddSingleton<IDbTenantAccessor, DefaultDbTenantAccessor>();
        services.TryAddSingleton<IDbClock, SystemDbClock>();

        services.TryAddSingleton<IShardingRouteResolver, DefaultShardingRouteResolver>();

        services.TryAddSingleton(typeof(ConfigureExternalServices), _ =>
        {
            return new ConfigureExternalServices
            {
                EntityService = (c, p) =>
                {
                    if (!p.IsPrimarykey && c.PropertyType.IsGenericType && c.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)
                        || (!p.IsPrimarykey && p.PropertyInfo.PropertyType == typeof(string)))
                    {
                        if (p.IsNullable)
                        {
                            p.IsNullable = true;
                        }
                    }
                }
            };
        });

        services.AddDbClient(dbType, configDb);

        services.TryAddScoped<IDbContext, SqlSugarDbContext>();


        var builder = new SqlSugarServiceBuilder(services);

        services.AddSingleton(builder);
        return builder;
    }

    private static void AddDbClient(this IServiceCollection services, DbType dbType, Action<IServiceProvider, ISqlSugarClient> configDb = null)
    {
        services.AddTransient(typeof(ISqlSugarClient), provider =>
        {
            var connectionManager = provider.GetRequiredService<IDbConnectionStringManager>();
            var configureExternalServices = provider.GetRequiredService<ConfigureExternalServices>();
            var configHandler = provider.GetRequiredService<DefaultSqlSugarConfigHandler>();

            SqlSugarClient db = new CustomSqlSugarClient(new ConnectionConfig
            {
                ConfigId = connectionManager.Current,
                ConnectionString = connectionManager.GetCurrentConnectionString(),
                DbType = dbType,
                InitKeyType = InitKeyType.Attribute,
                IsAutoCloseConnection = true,
                LanguageType = LanguageType.English,

                MoreSettings = new ConnMoreSettings
                {
                    DisableNvarchar = true
                },
                ConfigureExternalServices = configureExternalServices
            });

            db.Aop.OnDiffLogEvent = configHandler.OnDiffLogEvent;
            db.Aop.OnLogExecuting = configHandler.OnLogExecuting;
            db.Aop.OnExecutingChangeSql = configHandler.OnExecutingChangeSql;
            db.Aop.OnGetDataReadered = configHandler.OnGetDataReadered;
            db.Aop.OnGetDataReadering = configHandler.OnGetDataReadering;
            db.Aop.CheckConnectionExecuting = configHandler.CheckConnectionExecuting;
            db.Aop.CheckConnectionExecuted = configHandler.CheckConnectionExecuted;
            db.Aop.OnLogExecuted = (sql, parameters) => { configHandler.OnLogExecuted(dbType, sql, parameters, db.Ado.SqlExecutionTime.Milliseconds); };
            db.Aop.DataExecuting = configHandler.DataExecuting;
            db.Aop.OnError = configHandler.OnError;
            db.Aop.DataChangesExecuted = configHandler.DataChangesExecuted;
            db.Aop.DataExecuted = configHandler.DataExecuted;

            configHandler.DbSetting(db);

            configDb?.Invoke(provider, db);

            return db;
        });
    }
}

public class CustomSqlSugarClient : SqlSugarClient
{
    public CustomSqlSugarClient(ConnectionConfig config) : base(config)
    {
    }

    public CustomSqlSugarClient(List<ConnectionConfig> configs) : base(configs)
    {
    }

    public CustomSqlSugarClient(ConnectionConfig config, Action<SqlSugarClient> configAction) : base(config, configAction)
    {
    }

    public CustomSqlSugarClient(List<ConnectionConfig> configs, Action<SqlSugarClient> configAction) : base(configs, configAction)
    {
    }
}

public class DefaultSqlSugarConfigHandler
{
    private const string InfoTemplate = "ExecuteCommand[{ms}] Timeout[{timeout}]\r\n{sql}";

    private readonly IDbAuditAccessor _auditAccessor;
    private readonly IDbTenantAccessor _tenantAccessor;
    private readonly IDbClock _dbClock;
    private readonly IContextSnapshotManager<DbOperationState> _snapshotManager;
    private readonly DbOptions _sqlSugarOptions;
    protected ILogger<ISqlSugarClient> Logger { get; }

    public DefaultSqlSugarConfigHandler(IServiceProvider serviceProvider)
    {
        _auditAccessor = serviceProvider.GetRequiredService<IDbAuditAccessor>();
        _tenantAccessor = serviceProvider.GetRequiredService<IDbTenantAccessor>();
        _dbClock = serviceProvider.GetRequiredService<IDbClock>();
        _snapshotManager = serviceProvider.GetRequiredService<IContextSnapshotManager<DbOperationState>>();
        Logger = serviceProvider.GetRequiredService<ILogger<ISqlSugarClient>>();
        _sqlSugarOptions = serviceProvider.GetRequiredService<IOptions<DbOptions>>().Value;
    }

    public virtual void DbSetting(ISqlSugarClient db)
    {
        db.Ado.CommandTimeOut = _sqlSugarOptions.Timeout;

        //软删除
        db.QueryFilter.AddTableFilter<ISoftDeleted>(x => x.IsDeleted == false);

        //ITenantId租户处理
        var tenantId = _tenantAccessor.GetTenantId();
        db.QueryFilter.AddTableFilter<ITenantId>(x => x.TenantId == tenantId);
    }


    public virtual void DataExecuting(object obj, DataFilterModel model)
    {
        var opState = _snapshotManager.ContextAccessor.Current;
        if (!opState.IgnoreTenant //启用租户
            && model.PropertyName == nameof(ITenantId.TenantId) //当前为租户字段
            && model.EntityValue is ITenantId) //继承ITenantId
        {
            var v = model.EntityColumnInfo.PropertyInfo.GetValue(model.EntityValue);
            if (v == null)
            {
                var tenant = _tenantAccessor.GetTenantId();
                if (string.IsNullOrWhiteSpace(tenant))
                {
                    throw new InvalidOperationException("未能解析当前租户ID，请使用 [UseTenant] 指定租户或开启接口授权");
                }
                model.SetValue(tenant);
            }
        }

        //处理人员和时间字段
        switch (model.OperationType)
        {
            case DataFilterType.InsertByObject:
            {
                if (model.IsAnyAttribute<InsertDateTimeGenerationAttribute>())
                {
                    var v = model.EntityColumnInfo.PropertyInfo.GetValue(model.EntityValue);
                    if (v == null || (DateTime)v == default)
                    {
                        model.SetValue(_dbClock.Now);
                    }
                }

                if (model.IsAnyAttribute<InsertUserIdGenerationAttribute>())
                {
                    var v = model.EntityColumnInfo.PropertyInfo.GetValue(model.EntityValue);
                    if (v == null || v.Equals(0) || v.ToString() == string.Empty || v.Equals(Guid.Empty))
                    {
                        model.SetValue(_auditAccessor.GetUserId());
                    }
                }

                if (model.IsAnyAttribute<InsertUserNameGenerationAttribute>())
                {
                    var v = model.EntityColumnInfo.PropertyInfo.GetValue(model.EntityValue);
                    if (string.IsNullOrEmpty(v?.ToString()))
                    {
                        model.SetValue(_auditAccessor.GetUserName());
                    }
                }

                break;
            }
            case DataFilterType.UpdateByObject:
            {
                if (model.IsAnyAttribute<UpdateDateTimeGenerationAttribute>())
                {
                    var v = model.EntityColumnInfo.PropertyInfo.GetValue(model.EntityValue);
                    model.SetValue(_dbClock.Now);
                }

                if (model.IsAnyAttribute<UpdateUserIdGenerationAttribute>())
                {
                    model.SetValue(_auditAccessor.GetUserId());
                }

                if (model.IsAnyAttribute<UpdateUserNameGenerationAttribute>())
                {
                    model.SetValue(_auditAccessor.GetUserName());
                }

                break;
            }
        }
    }

    public virtual void OnError(SqlSugarException exception)
    {
        Logger?.LogError(exception, "DbError: {sql} . {message}", exception.Sql, exception.Message);
    }

    public virtual void OnDiffLogEvent(DiffLogModel diffLogModel)
    {
    }


    public virtual void OnLogExecuting(string x, SugarParameter[] parameters)
    {
    }

    public virtual void OnLogExecuted(DbType dbType, string sql, SugarParameter[] parameters, int ms)
    {
        //警告慢sql
        if (ms >= _sqlSugarOptions.SlowSqlTime * 1000)
            Logger.LogWarning(message: InfoTemplate, ms, _sqlSugarOptions.Timeout, sql);

        if (_sqlSugarOptions.EnableSqlLog)
        {
            sql = UtilMethods.GetSqlString(dbType, sql, parameters);
            Logger.LogInformation(message: InfoTemplate, ms, _sqlSugarOptions.Timeout, sql);
        }
    }

    public virtual KeyValuePair<string, SugarParameter[]> OnExecutingChangeSql(string sql, SugarParameter[] parameters)
    {
        return new KeyValuePair<string, SugarParameter[]>(sql, parameters);
    }


    public virtual void DataChangesExecuted(object obj, DataFilterModel model)
    {
    }

    public virtual void DataExecuted(object obj, DataAfterModel model)
    {
    }

    public virtual void CheckConnectionExecuting(IDbConnection dbConnection)
    {
    }

    public virtual void CheckConnectionExecuted(IDbConnection dbConnection, TimeSpan timeSpan)
    {
    }

    public virtual void OnGetDataReadering(string x, SugarParameter[] parameters)
    {
    }

    public virtual void OnGetDataReadered(string x, SugarParameter[] parameters, TimeSpan timeSpan)
    {
    }
}
