using Kurisu.Core.DataAccess.Entity;
using Kurisu.Core.User.Abstractions;
using Kurisu.SqlSugar.Attributes;
using Kurisu.SqlSugar.DiffLog;
using Kurisu.SqlSugar.Options;
using Kurisu.SqlSugar.Services;
using Kurisu.SqlSugar.Services.Implements;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlSugar;

namespace Kurisu.SqlSugar.Extensions;

/// <summary>
/// sqlsugar ioc 注入
/// </summary>
public static class SqlSugarServiceCollectionExtensions
{
    private readonly static string _infoTemplate = "ExecuteCommand[{ms}] Timeout[{timeout}] \r\n {sql}";

    /// <summary>
    /// 添加sqlsugar
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configOtherConnections"></param>
    public static void AddSqlSugar(this IServiceCollection services, Func<IServiceProvider, List<ConnectionConfig>> configOtherConnections = null)
    {
        services.AddScoped<ISqlSugarOptionsService, SqlSugarOptionsService>();
        services.AddScoped<DataPermissionService>();
        services.AddScoped<IDbContext, DbContext>();
        services.AddScoped(typeof(ISqlSugarClient), sp =>
        {
            var options = sp.GetService<IOptions<SqlSugarOptions>>().Value;
            var logger = sp.GetService<ILogger<SqlSugarClient>>();
            var currentUser = sp.GetService<ICurrentUser>();
            var sugarOptions = sp.GetService<ISqlSugarOptionsService>();
            var dataPermission = sp.GetService<DataPermissionService>();

            var configs = new List<ConnectionConfig>()
            {
                new ConnectionConfig
                {
                    ConfigId = "db-main",

                    ConnectionString = options.DefaultConnectionString,
                    DbType = DbType.MySql,
                    InitKeyType = InitKeyType.Attribute,
                    IsAutoCloseConnection = true,

                    MoreSettings = new ConnMoreSettings
                    {
                        DisableNvarchar = true,
                    },
                    ConfigureExternalServices = new ConfigureExternalServices
                    {
                        EntityService = (c, p) =>
                        {
                            if (!p.IsPrimarykey && c.PropertyType.IsGenericType && c.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)
                            || (!p.IsPrimarykey && p.PropertyInfo.PropertyType == typeof(string)))
                            {
                                p.IsNullable = true;
                            }
                        }
                    }
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
                    logger.LogWarning(message: _infoTemplate, ms, options.Timeout, sql);

                if (options.EnableSqlLog)
                {
                    sql = UtilMethods.GetSqlString(DbType.MySql, sql, parameters);
                    logger.LogInformation(message: _infoTemplate, ms, options.Timeout, sql);
                }
            };

            //ITenantId租户处理
            if (currentUser != null)
            {
                var tenantId = currentUser.GetStringTenantId();
                db.QueryFilter.AddTableFilter<ITenantId>(x => x.TenantId == tenantId);
            }

            //软删除
            db.QueryFilter.AddTableFilter<ISoftDeleted>(x => x.IsDeleted == false);


            //#if DEBUG
            //SQL执行前
            //db.Aop.OnLogExecuting = (sql, parameters) =>
            //{
            //    logger.LogInformation(UtilMethods.GetSqlString(DbType.SqlServer, sql, parameters));
            //};
            //#endif

            //操作数据权限

            db.Aop.OnExecutingChangeSql = (sql, parameters) =>
            {
                if (dataPermission.Enable && dataPermission.UseSqlWhere)
                {
                    sql += $"\r\n and ({string.Join(" and ", dataPermission.Wheres)})";
                }
                return new KeyValuePair<string, SugarParameter[]>(sql, parameters);
            };



            //增删改
            db.Aop.DataExecuting = (oldValue, model) =>
            {
                if (sugarOptions.IgnoreTenant == false //启用租户
                && currentUser != null //用户信息存在
                && model.PropertyName == nameof(ITenantId.TenantId) //当前为租户字段
                && typeof(ITenantId).IsAssignableFrom(model.EntityValue.GetType())) //继承ITenantId
                {
                    var tenant = currentUser.GetStringTenantId();
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
                                if ((int)model.EntityColumnInfo.PropertyInfo.GetValue(model.EntityValue) == 0)
                                {
                                    model.SetValue(currentUser.GetStringSubjectId());
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
                                if ((int)model.EntityColumnInfo.PropertyInfo.GetValue(model.EntityValue) == 0)
                                {
                                    model.SetValue(currentUser.GetStringSubjectId());
                                }
                            }

                            break;
                        }
                }
            };

            //对查询后的数据处理(加解密处理)
            //db.Aop.DataExecuted = (oldValue, entity) => { };

            if (options.Diff?.Enable == true && options.Diff?.Commands?.Any() == true)
            {
                //数据差异
                db.Aop.OnDiffLogEvent = (diffModel) =>
                {
                    var changes = ColumnsChangeHelper.GetChanges(sp, diffModel);
                    if (changes.Any())
                    {
                        var diffDb = sp.CreateScope().ServiceProvider.GetService<IDbContext>();
                        diffDb.Client.Ado.Connection.ConnectionString = options.Diff.LogConnectionString;
                        diffDb.Client.CodeFirst.InitTables<TableRowChangeEntity>();

                        diffDb.Client.Insertable(changes).ExecuteCommand();
                    }
                };
            }

            return db;
        });
    }
}