using Kurisu.Core.User.Abstractions;
using Kurisu.SqlSugar.DiffLog.Contants;
using Kurisu.SqlSugar.DiffLog.Models;
using Kurisu.SqlSugar.Options;
using Kurisu.SqlSugar.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SqlSugar;

namespace Kurisu.SqlSugar.DiffLog;

internal class ColumnsChangeHelper
{
    public static List<TableColumnChangesEntity> GetChanges(IServiceProvider sp, DiffLogModel model)
    {
        var operation = model.DiffType.ToString();
        var dbOptions = sp.GetService<IOptions<SqlSugarOptions>>().Value;
        if (dbOptions.Diff?.Enable != true || dbOptions.Diff?.Commands?.Any() != true || dbOptions.Diff?.Commands?.Contains(operation) != true)
        {
            return new List<TableColumnChangesEntity>();
        }

        var optionsService = sp.GetService<ISqlSugarOptionsService>();
        var currentUser = sp.GetService<ICurrentUser>();

        var entries = new List<TableColumnChangesEntity>();

        var uid = Guid.NewGuid();
        var befores = model.BeforeData;
        var afters = model.AfterData;

        var beforeUnits = befores?.Select(x => new DiffTableModel
        {
            Table = x.TableName,
            Columns = x.Columns.Select(c => new DiffColumnModel
            {
                Column = c.ColumnName,
                IsPrimaryKey = c.IsPrimaryKey,
                Value = c.Value?.ToString() ?? string.Empty
            })
            .OrderByDescending(x => x.IsPrimaryKey)
            .ToList()

        }).ToList();

        var afterUnits = afters?.Select(x => new DiffTableModel
        {
            Table = x.TableName,
            Columns = x.Columns.Select(c => new DiffColumnModel
            {
                Column = c.ColumnName,
                IsPrimaryKey = c.IsPrimaryKey,
                Value = c.Value?.ToString() ?? string.Empty
            })
            .OrderByDescending(x => x.IsPrimaryKey)
            .ToList()

        }).ToList() ?? new List<DiffTableModel>();

        foreach (var item in afterUnits)
        {
            var table = item.Table;
            var keyValue = item.Columns.FirstOrDefault(x => x.IsPrimaryKey)?.Value;

            var changeInfos = new List<ColumnChangesDetail>();

            if (string.IsNullOrEmpty(keyValue))
            {
                continue;
            }

            if (beforeUnits?.Any() == true)
            {
                var before = beforeUnits.FirstOrDefault(x =>
                         x.Table.Equals(table)
                      && x.Columns.Where(c => c.IsPrimaryKey && c.Value.Equals(keyValue)).Any());

                foreach (var column in item.Columns)
                {
                    var c = before.Columns.FirstOrDefault(x => x.Column == column.Column);
                    if (c.Value != column.Value)
                    {
                        changeInfos.Add(new ColumnChangesDetail
                        {
                            Column = column.Column,
                            Detail = c.Value + CombineConsts.Arrow + column.Value
                        });
                    }
                }
            }
            else
            {
                foreach (var column in item.Columns)
                {
                    changeInfos.Add(new ColumnChangesDetail
                    {
                        Column = column.Column,
                        Detail = column.Value
                    });
                }
            }

            if (changeInfos.Any())
            {
                entries.Add(new TableColumnChangesEntity
                {
                    RoutePath = optionsService?.RoutePath ?? string.Empty,
                    Title = optionsService?.Title ?? string.Empty,
                    TableName = FixTableName(model.Sql),
                    Operation = operation,
                    BatchNo = optionsService?.BatchNo ?? uid,
                    KeyValue = FixWhere(model.DiffType, model.Sql, model.Parameters),
                    Changes = changeInfos,
                    CreateTime = optionsService.RaiseTime,
                    CreatedBy = currentUser?.GetIntSubjectId() == 0 ? 1 : currentUser.GetIntSubjectId(),
                    CreatedByName = currentUser?.GetName() ?? "管理员",
                    ModifiedBy = currentUser?.GetIntSubjectId() == 0 ? 1 : currentUser.GetIntSubjectId(),
                    ModifiedTime = optionsService.RaiseTime,
                    TenantId = currentUser.GetStringTenantId(),
                });
            }
        }

        if (!entries.Any() && model.DiffType == DiffType.delete)
        {
            entries.Add(new TableColumnChangesEntity
            {
                RoutePath = optionsService?.RoutePath ?? string.Empty,
                Title = optionsService?.Title ?? string.Empty,
                TableName = FixTableName(model.Sql),
                Operation = operation,
                BatchNo = optionsService?.BatchNo ?? uid,
                KeyValue = FixWhere(model.DiffType, model.Sql, model.Parameters),
                Changes = new List<ColumnChangesDetail>(),
                CreateTime = optionsService.RaiseTime,
                CreatedBy = currentUser?.GetIntSubjectId() == 0 ? 1 : currentUser.GetIntSubjectId(),
                CreatedByName = currentUser?.GetName() ?? "管理员",
                ModifiedBy = currentUser?.GetIntSubjectId() == 0 ? 1 : currentUser.GetIntSubjectId(),
                ModifiedTime = optionsService.RaiseTime,
                TenantId = currentUser.GetStringTenantId(),
            });
        }

        return entries.Where(x => x.Changes.Any()).ToList();
    }

    private static string FixTableName(string sql)
    {
        var tableName = sql.Split(" ")[2].TrimStart('`').TrimEnd('`');
        if (string.IsNullOrEmpty(tableName))
        {
            tableName = sql.Split(" ")[1].TrimStart('[').TrimEnd(']');
        }

        return tableName;
    }

    private static string FixWhere(DiffType diffType, string sql, SugarParameter[] parameters)
    {
        if (diffType == DiffType.insert)
        {
            return string.Empty;
        }
        var arr = sql.Split(" ");
        arr = arr.Skip(3).Take(arr.Length - 3).ToArray();

        string where = string.Join(" ", arr);

        foreach (var item in parameters)
        {
            where = where.Replace(item.ParameterName, "'" + item.Value.ToString() + "'");
        }

        return where;
    }
}
