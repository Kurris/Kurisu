using System;
using System.Collections.Generic;
using System.Data;
using Kurisu.AspNetCore.Authentication.Abstractions;
using Kurisu.AspNetCore.DataAccess.Entity;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Attributes;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Options;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Services;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Services.Implements;
using Kurisu.AspNetCore.UnifyResultAndValidation.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlSugar;
using DbType = SqlSugar.DbType;

namespace Kurisu.AspNetCore.DataAccess.SqlSugar.Extensions;

/// <summary>
/// SqlSugar Ioc 注入
/// </summary>
public static class SqlSugarServiceCollectionExtensions
{
    /// <summary>
    /// 添加SQLSugar
    /// </summary>
    /// <param name="services"></param>
    public static void AddSqlSugar(this IServiceCollection services)
    {
        services.AddSqlSugar(DbType.MySqlConnector);
    }

    /// <summary>
    /// 添加SQLSugar
    /// </summary>
    /// <param name="services"></param>
    /// <param name="dbType"></param>
    public static void AddSqlSugar(this IServiceCollection services, DbType dbType)
    {
        services.AddSqlSugar(dbType, null, null);
    }

    public static void AddSqlSugar(this IServiceCollection services, DbType dbType, Func<IServiceProvider, List<ConnectionConfig>> configOtherConnections)
    {
        services.AddSqlSugar(dbType, configOtherConnections, null);
    }

    public static void AddSqlSugar(this IServiceCollection services, DbType dbType, Action<ISqlSugarClient> configDb)
    {
        services.AddSqlSugar(dbType, null, configDb);
    }

    /// <summary>
    /// 添加SQLSugar
    /// </summary>
    /// <param name="services"></param>
    /// <param name="dbType"></param>
    /// <param name="configOtherConnections"></param>
    /// <param name="configDb"></param>
    public static void AddSqlSugar(this IServiceCollection services, DbType dbType, Func<IServiceProvider, List<ConnectionConfig>> configOtherConnections, Action<ISqlSugarClient> configDb)
    {
        services.AddSqlSugarCore();
        services.AddScoped(typeof(ISqlSugarClient), sp => SqlSugarServiceCollectionExtensionsHelper.ConfigDb(sp, dbType, configOtherConnections, configDb));
    }

    private static void AddSqlSugarCore(this IServiceCollection services)
    {
        services.AddScoped<IDbConnectionFactory, DefaultConnectionHandler>();
        services.AddScoped<ISqlSugarOptionsService, SqlSugarOptionsService>();
        services.AddScoped<IQueryableSetting, QueryableSetting>();
        services.AddScoped<IDbContext, DbContext>();
        services.AddSingleton<IIsolationLevelService, IsolationLevelService>();
        services.AddSingleton(_ => SqlSugarServiceCollectionExtensionsHelper.ConfigExternal());
    }
}

internal static class SqlSugarServiceCollectionExtensionsHelper
{
    private const string InfoTemplate = "ExecuteCommand[{ms}] Timeout[{timeout}]\r\n{sql}";

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
        Func<IServiceProvider, List<ConnectionConfig>> configOtherConnections,
        Action<ISqlSugarClient> configDb)
    {
        var options = sp.GetService<IOptions<SqlSugarOptions>>().Value;
        var logger = sp.GetService<ILogger<SqlSugarClient>>();
        var currentUser = sp.GetService<ICurrentUser>();
        var sugarOptions = sp.GetService<ISqlSugarOptionsService>();
        var isolationLevel = sp.GetService<IIsolationLevelService>();
        var configureExternalServices = sp.GetService<ConfigureExternalServices>();
        var connectionHandler = sp.GetService<IDbConnectionFactory>();

        switch (dbType)
        {
            case DbType.MySql or DbType.MySqlConnector:
                isolationLevel.Set(IsolationLevel.RepeatableRead);
                break;
            case DbType.SqlServer:
                isolationLevel.Set(IsolationLevel.ReadCommitted);
                break;
        }

        var configs = new List<ConnectionConfig>
        {
            new()
            {
                ConfigId = "default",
                ConnectionString = connectionHandler.GetConnectionString(),
                DbType = dbType,
                InitKeyType = InitKeyType.Attribute,
                IsAutoCloseConnection = true,

                MoreSettings = new ConnMoreSettings
                {
                    DisableNvarchar = true
                },
                ConfigureExternalServices = configureExternalServices
            }
        };

        //添加外部连接
        var otherConfigs = configOtherConnections?.Invoke(sp);
        if (otherConfigs != null)
        {
            configs.AddRange(otherConfigs);
        }

        ISqlSugarClient db = new SqlSugarClient(configs);

        db.Ado.CommandTimeOut = options.Timeout;

        db.Aop.OnLogExecuted = (sql, parameters) =>
        {
            var ms = db.Ado.SqlExecutionTime.Milliseconds;

            //警告慢sql
            if (ms >= options.SlowSqlTime * 1000)
                logger.LogWarning(message: InfoTemplate, ms, options.Timeout, sql);

            if (options.EnableSqlLog)
            {
                sql = UtilMethods.GetSqlString(dbType, sql, parameters);
                logger.LogInformation(message: InfoTemplate, ms, options.Timeout, sql);
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
                        if (v == null || (string)v == string.Empty || v.Equals(Guid.Empty) || v.Equals(0))
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
            logger.LogError(exception, "DbError:{message}", exception.Message);
            throw new UserFriendlyException($"DbError:{exception.Message}");
        };

        configDb?.Invoke(db);

        return db;
    }
}