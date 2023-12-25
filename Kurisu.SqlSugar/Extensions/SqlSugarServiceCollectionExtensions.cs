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

public static class SqlSugarServiceCollectionExtensions
{
    public static void AddSqlSugar(this IServiceCollection services, Action<List<TableColumnChangesEntity>> toLogDiffAction = null)
    {
        services.AddScoped<ISqlSugarOptionsService, SqlSugarOptionsService>();
        services.AddScoped<IDbContext, DbContext>();
        services.AddScoped(typeof(ISqlSugarClient), sp =>
        {
            var options = sp.GetService<IOptions<SqlSugarOptions>>().Value;
            var logger = sp.GetService<ILogger<SqlSugarClient>>();

            var config = new ConnectionConfig
            {
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
            };

            ISqlSugarClient db = new SqlSugarClient(config);

            db.Ado.CommandTimeOut = options.Timeout;

            if (options.EnableSqlLog)
            {
                db.Aop.OnLogExecuted = (sql, parameters) =>
                {
                    var ms = db.Ado.SqlExecutionTime.Milliseconds;
                    var info = "ExecuteCommand[{MS}] Timeout[{Timeout}] \r\n {Sql}";

                    //警告慢sql
                    if (ms >= options.SlowSqlTime * 1000)
                        logger.LogWarning(info, ms, options.Timeout, sql);
                    else
                        logger.LogInformation(info, ms, options.Timeout, sql);
                };
            }

            //租户处理
            var currentUser = sp.GetService<ICurrentUser>();
            if (currentUser != null)
            {
                var tenantId = currentUser.GetStringTenantId();
                db.QueryFilter.AddTableFilter<ITenantId>(x => x.TenantId == tenantId);
            }

            //软删除
            db.QueryFilter.AddTableFilter<ISoftDeleted>(x => x.IsDeleted == false);


            //#if DEBUG
            ////SQL执行前
            //db.Aop.OnLogExecuting = (sql, parameters) =>
            //{
            //    logger.LogInformation(UtilMethods.GetSqlString(DbType.SqlServer, sql, parameters));
            //};
            //#endif
            //操作数据权限
            //db.Aop.OnExecutingChangeSql = (sql, parameters) => new KeyValuePair<string, SugarParameter[]>(sql, parameters);


            //增删改
            db.Aop.DataExecuting = (oldValue, entity) =>
            {
                var currentUser = sp.GetService<ICurrentUser>();

                if (currentUser != null && entity.PropertyName == nameof(ITenantId.TenantId) && typeof(ITenantId).IsAssignableFrom(entity.EntityValue.GetType()))
                {
                    var hotelId = currentUser.GetStringTenantId();
                    entity.SetValue(hotelId);
                }

                if (entity.OperationType == DataFilterType.InsertByObject)
                {
                    if (entity.IsAnyAttribute<InsertDateTimeGenerationAttribute>())
                    {
                        entity.SetValue(DateTime.Now);
                    }

                    if (currentUser != null && entity.IsAnyAttribute<InsertUserGenerationAttribute>())
                    {
                        entity.SetValue(currentUser.GetStringSubjectId());
                    }
                }
                else if (entity.OperationType == DataFilterType.UpdateByObject)
                {
                    if (entity.IsAnyAttribute<UpdateDateTimeGenerationAttribute>())
                    {
                        entity.SetValue(DateTime.Now);
                    }

                    if (currentUser != null && entity.IsAnyAttribute<UpdateUserGenerationAttribute>())
                    {
                        entity.SetValue(currentUser.GetStringSubjectId());
                    }
                }
            };

            //对查询后的数据处理(加解密处理)
            db.Aop.DataExecuted = (oldValue, entity) => { };

            if (options.Diff?.Enable == true)
            {
                //数据差异
                db.Aop.OnDiffLogEvent = (diffModel) =>
                {
                    var changes = ColumnsChangeHelper.GetChanges(sp, diffModel);
                    if (changes.Any() && toLogDiffAction != null)
                    {
                        toLogDiffAction(changes);
                    }
                };
            }

            return db;
        });
    }
}