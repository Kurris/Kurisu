using Kurisu.AspNetCore.Abstractions.Authentication;
using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.Abstractions.DataAccess.Contract;
using Kurisu.AspNetCore.Abstractions.UnifyResultAndValidation;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Attributes;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Options;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Services;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Services.Implements;
using Kurisu.Extensions.SqlSugar.Services;
using Kurisu.Extensions.SqlSugar.Services.Implements;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlSugar;
using DbType = SqlSugar.DbType;

namespace Kurisu.Extensions.SqlSugar.Extensions;

/// <summary>
/// SqlSugar Ioc 注入
/// </summary>
public static class SqlSugarServiceCollectionExtensions
{
    private static void AddSqlSugarCore(this IServiceCollection services)
    {
        services.AddScoped<IDatasourceManager, SqlSugarDatasourceManager>();
        services.AddScoped<IDbConnectionManager, SqlSugarConnectionHandler>();
        services.AddScoped<ISqlSugarOptionsService, SqlSugarOptionsService>();
        services.AddScoped<IQueryableSetting, QueryableSetting>();
        services.AddScoped<IDbContext, SqlSugarDbContext>();
        services.AddSingleton(_ => SqlSugarServiceCollectionExtensionsHelper.ConfigExternal());
    }

    /// <summary>
    /// 添加SQLSugar
    /// </summary>
    /// <param name="services"></param>
    public static void AddSqlSugar(this IServiceCollection services)
    {
        services.AddSqlSugar(DbType.MySqlConnector);
    }

    public static void AddSqlSugar(this IServiceCollection services, DbType dbType)
    {
        services.AddSqlSugar(dbType, null, null);
    }


    public static void AddSqlSugar(this IServiceCollection services, DbType dbType, Func<IServiceProvider, DbType, ConnectionConfig> configOtherConnection)
    {
        services.AddSqlSugar(dbType, configOtherConnection, null);
    }

    public static void AddSqlSugar(this IServiceCollection services, DbType dbType, Action<IServiceProvider, ISqlSugarClient> configDb)
    {
        services.AddSqlSugar(dbType, null, configDb);
    }


    /// <summary>
    /// 添加SQLSugar
    /// </summary>
    /// <param name="services"></param>
    /// <param name="dbType"></param>
    /// <param name="configOtherConnection"></param>
    /// <param name="configDb"></param>
    public static void AddSqlSugar(this IServiceCollection services, DbType dbType,
        Func<IServiceProvider, DbType, ConnectionConfig> configOtherConnection,
        Action<IServiceProvider, ISqlSugarClient> configDb)
    {
        services.AddSqlSugarCore();
        services.AddTransient(typeof(ISqlSugarClient), provider =>
            SqlSugarServiceCollectionExtensionsHelper.ConfigDb(provider, dbType,
                configOtherConnection ??= (sp, type) =>
                {
                    var configureExternalServices = sp.GetService<ConfigureExternalServices>();
                    var connectionHandler = sp.GetRequiredService<IDbConnectionManager>();

                    return new ConnectionConfig
                    {
                        ConfigId = connectionHandler.GetCurrent(),
                        ConnectionString = connectionHandler.GetCurrentConnectionString(),
                        DbType = type,
                        InitKeyType = InitKeyType.Attribute,
                        IsAutoCloseConnection = true,

                        MoreSettings = new ConnMoreSettings
                        {
                            DisableNvarchar = true
                        },
                        ConfigureExternalServices = configureExternalServices
                    };
                },
                configDb ??= (sp, db) =>
                {
                    const string infoTemplate = "ExecuteCommand[{ms}] Timeout[{timeout}]\r\n{sql}";

                    var options = sp.GetRequiredService<IOptions<SqlSugarOptions>>().Value;
                    var logger = sp.GetService<ILogger<SqlSugarClient>>();
                    var currentUser = sp.GetService<ICurrentUser>();
                    var sugarOptions = sp.GetRequiredService<ISqlSugarOptionsService>();

                    db.Ado.CommandTimeOut = options.Timeout;

                    db.Aop.OnLogExecuted = (sql, parameters) =>
                    {
                        var ms = db.Ado.SqlExecutionTime.Milliseconds;

                        //警告慢sql
                        if (ms >= options.SlowSqlTime * 1000)
                            logger.LogWarning(message: infoTemplate, ms, options.Timeout, sql);

                        if (options.EnableSqlLog)
                        {
                            sql = UtilMethods.GetSqlString(dbType, sql, parameters);
                            logger.LogInformation(message: infoTemplate, ms, options.Timeout, sql);
                        }
                    };

                    //ITenantId租户处理
                    if (currentUser != null)
                    {
                        var tenantId = currentUser.GetTenantId();
                        db.QueryFilter.AddTableFilter<ITenantId>(x => x.TenantId == tenantId);
                    }

                    //软删除
                    db.QueryFilter.AddTableFilter<ISoftDeleted>(x => x.IsDeleted == false);

                    db.Aop.DataExecuting = (_, model) =>
                    {
                        if (!sugarOptions.IgnoreTenant //启用租户
                            && currentUser != null //用户信息存在
                            && model.PropertyName == nameof(ITenantId.TenantId) //当前为租户字段
                            && model.EntityValue is ITenantId) //继承ITenantId
                        {
                            var tenant = currentUser.GetTenantId();
                            model.SetValue(tenant);
                        }

                        //处理人员和时间字段
                        switch (model.OperationType)
                        {
                            case DataFilterType.InsertByObject:
                            {
                                if (model.IsAnyAttribute<InsertDateTimeGenerationAttribute>())
                                {
                                    model.SetValue(DateTime.Now);
                                }

                                if (currentUser != null && model.IsAnyAttribute<InsertUserGenerationAttribute>())
                                {
                                    var v = model.EntityColumnInfo.PropertyInfo.GetValue(model.EntityValue);
                                    if (v == null || v.Equals(0) || v.ToString() == string.Empty || v.Equals(Guid.Empty))
                                    {
                                        model.SetValue(currentUser.GetUserId());
                                    }
                                }

                                break;
                            }
                            case DataFilterType.UpdateByObject:
                            {
                                if (model.IsAnyAttribute<UpdateDateTimeGenerationAttribute>())
                                {
                                    model.SetValue(DateTime.Now);
                                }

                                if (currentUser != null && model.IsAnyAttribute<UpdateUserGenerationAttribute>())
                                {
                                    model.SetValue(currentUser.GetUserId());
                                }

                                break;
                            }
                        }
                    };

                    db.Aop.OnError = exception =>
                    {
                        logger?.LogError(exception, "DbError: {sql} . {message}", exception.Sql, exception.Message);
                        throw new UserFriendlyException($"DbError:{exception.Message}");
                    };
                })
        );
    }
}

internal static class SqlSugarServiceCollectionExtensionsHelper
{
    public static ConfigureExternalServices ConfigExternal()
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
    }


    public static ISqlSugarClient ConfigDb(IServiceProvider sp, DbType dbType,
        Func<IServiceProvider, DbType, ConnectionConfig> configConnection,
        Action<IServiceProvider, ISqlSugarClient> configDb)
    {
        var connectionConfig = configConnection(sp, dbType);
        ISqlSugarClient db = new SqlSugarClient(connectionConfig);
        configDb(sp, db);

        return db;
    }
}