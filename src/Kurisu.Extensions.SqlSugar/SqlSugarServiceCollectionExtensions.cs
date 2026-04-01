using System.Data;
using Kurisu.AspNetCore.Abstractions.Authentication;
using Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.AspNetCore.Abstractions.Result;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Attributes;
using Kurisu.Extensions.ContextAccessor;
using Kurisu.Extensions.ContextAccessor.Abstractions;
using Kurisu.Extensions.SqlSugar.Attributes;
using Kurisu.Extensions.SqlSugar.Core.Context;
using Kurisu.Extensions.SqlSugar.Core.Manager;
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
        services.TryAddScoped<IDatasourceManager<ISqlSugarClient>, SqlSugarDatasourceManager>();
        services.TryAddScoped<IDatasourceManager>(sp => sp.GetRequiredService<IDatasourceManager<ISqlSugarClient>>());

        services.TryAddSingleton<DefaultSqlSugarConfigHandler>();

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

            db.Aop.OnDiffLogEvent = diffLogModel => { configHandler.OnDiffLogEvent(diffLogModel); };
            db.Aop.OnLogExecuting = (x, parameters) => { configHandler.OnLogExecuting(x, parameters); };
            db.Aop.OnExecutingChangeSql = (x, parameters) => configHandler.OnExecutingChangeSql(x, parameters);
            db.Aop.OnGetDataReadered = (x, parameters, timeSpan) => { configHandler.OnGetDataReadered(x, parameters, timeSpan); };
            db.Aop.OnGetDataReadering = (x, parameters) => { configHandler.OnGetDataReadering(x, parameters); };
            db.Aop.CheckConnectionExecuting = dbConnection => { configHandler.CheckConnectionExecuting(dbConnection); };
            db.Aop.CheckConnectionExecuted = (dbConnection, timeSpan) => { configHandler.CheckConnectionExecuted(dbConnection, timeSpan); };
            db.Aop.OnLogExecuted = (sql, parameters) => { configHandler.OnLogExecuted(dbType, sql, parameters, db.Ado.SqlExecutionTime.Milliseconds); };
            db.Aop.DataExecuting = (obj, model) => { configHandler.DataExecuting(obj, model); };
            db.Aop.OnError = exception => { configHandler.OnError(exception); };
            db.Aop.DataChangesExecuted = (obj, model) => { configHandler.DataChangesExecuted(obj, model); };
            db.Aop.DataExecuted = (obj, model) => { configHandler.DataExecuted(obj, model); };

            configHandler.DbSetting(db);

            configDb?.Invoke(provider, db);

            return db;
        });
    }
}

public class CustomSqlSugarClient : SqlSugarClient, IDisposable
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

    /// <summary>
    /// 只在调试生命周期使用
    /// </summary>
    public new void Dispose()
    {
        base.Dispose();
    }
}

public class DefaultSqlSugarConfigHandler
{
    private const string InfoTemplate = "ExecuteCommand[{ms}] Timeout[{timeout}]\r\n{sql}";

    private readonly ICurrentUser _currentUser;
    private readonly IContextSnapshotManager<DbOperationState> _snapshotManager;
    private readonly DbOptions _sqlSugarOptions;
    protected ILogger<ISqlSugarClient> Logger { get; }

    public DefaultSqlSugarConfigHandler(IServiceProvider serviceProvider)
    {
        _currentUser = serviceProvider.GetRequiredService<ICurrentUser>();
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
        if (_currentUser != null)
        {
            var tenantId = _currentUser.GetTenantId();
            db.QueryFilter.AddTableFilter<ITenantId>(x => x.TenantId == tenantId);
        }
    }


    public virtual void DataExecuting(object obj, DataFilterModel model)
    {
        var opState = _snapshotManager.ContextAccessor.Current;
        if (!opState.IgnoreTenant //启用租户
            && _currentUser != null //用户信息存在
            && model.PropertyName == nameof(ITenantId.TenantId) //当前为租户字段
            && model.EntityValue is ITenantId) //继承ITenantId
        {
            var v = model.EntityColumnInfo.PropertyInfo.GetValue(model.EntityValue);
            if (v == null)
            {
                var tenant = _currentUser.GetTenantId();
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
                            model.SetValue(DateTime.Now);
                        }
                    }

                    if (_currentUser != null && model.IsAnyAttribute<InsertUserIdGenerationAttribute>())
                    {
                        var v = model.EntityColumnInfo.PropertyInfo.GetValue(model.EntityValue);
                        if (v == null || v.Equals(0) || v.ToString() == string.Empty || v.Equals(Guid.Empty))
                        {
                            model.SetValue(_currentUser.GetUserId());
                        }
                    }

                    if (_currentUser != null && model.IsAnyAttribute<InsertUserNameGenerationAttribute>())
                    {
                        var v = model.EntityColumnInfo.PropertyInfo.GetValue(model.EntityValue);
                        if (string.IsNullOrEmpty(v?.ToString()))
                        {
                            model.SetValue(_currentUser.GetName());
                        }
                    }

                    break;
                }
            case DataFilterType.UpdateByObject:
                {
                    if (model.IsAnyAttribute<UpdateDateTimeGenerationAttribute>())
                    {
                        var v = model.EntityColumnInfo.PropertyInfo.GetValue(model.EntityValue);
                        if (v == null || (DateTime)v == default)
                        {
                            model.SetValue(DateTime.Now);
                        }
                    }

                    if (_currentUser != null && model.IsAnyAttribute<UpdateUserIdGenerationAttribute>())
                    {
                        model.SetValue(_currentUser.GetUserId());
                    }

                    if (_currentUser != null && model.IsAnyAttribute<UpdateUserNameGenerationAttribute>())
                    {
                        model.SetValue(_currentUser.GetName());
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