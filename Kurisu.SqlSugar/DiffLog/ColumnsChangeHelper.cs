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
    private readonly static List<string> _ignoreColumns = new() { nameof(SugarBaseEntity.ModifiedTime) };

    public static List<TableRowChangeEntity> GetChanges(IServiceProvider sp, DiffLogModel model)
    {
        var operation = model.DiffType.ToString();

        var dbOptions = sp.GetService<IOptions<SqlSugarOptions>>().Value;
        if (dbOptions.Diff.Commands.Contains(operation) != true)
        {
            return new List<TableRowChangeEntity>();
        }

        var optionsService = sp.GetService<ISqlSugarOptionsService>();
        var currentUser = sp.GetService<ICurrentUser>();

        var entries = new List<TableRowChangeEntity>();

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
                    if (_ignoreColumns.Contains(column.Column))
                    {
                        continue;
                    }

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
                    if (_ignoreColumns.Contains(column.Column))
                    {
                        continue;
                    }

                    changeInfos.Add(new ColumnChangesDetail
                    {
                        Column = column.Column,
                        Detail = column.Value
                    });
                }
            }

            if (changeInfos.Any())
            {
                entries.Add(new TableRowChangeEntity
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
            entries.Add(new TableRowChangeEntity
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
        var tableName = sql.Split(" ")[2].TrimStart('[').TrimEnd(']').TrimStart('`').TrimEnd('`');
        if (string.IsNullOrEmpty(tableName))
        {
            tableName = sql.Split(" ")[1].TrimStart('[').TrimEnd(']').TrimStart('`').TrimEnd('`');
        }

        return tableName;
    }

    private static string FixWhere(DiffType diffType, string sql, SugarParameter[] parameters)
    {
        var arr = sql.Split(" ");

        if (diffType == DiffType.insert)
        {
            return string.Empty;
        }
        else if (diffType == DiffType.update)
        {
            var w = sql.Split("WHERE")[1];
            foreach (var item in parameters)
            {
                w = w.Replace(item.ParameterName, "'" + item.Value.ToString() + "'");
            }
            return w;
        }


        arr = arr.Skip(3).Take(arr.Length - 3).ToArray();

        string where = string.Join(" ", arr);

        foreach (var item in parameters)
        {
            where = where.Replace(item.ParameterName, "'" + item.Value.ToString() + "'");
        }

        return where;
    }
}
