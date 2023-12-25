﻿using System.ComponentModel.DataAnnotations;
using Kurisu.Core.ConfigurableOptions.Attributes;
using SqlSugar;

namespace Kurisu.SqlSugar.Options;

[Configuration("SqlSugarOptions:Diff")]
public class DiffOptions : IValidatableObject
{
    public bool Enable { get; set; }

    /// <summary>
    /// 日志数据库连接字符串
    /// </summary>
    public string LogConnectionString { get; set; }

    /// <summary>
    /// 日志记录sql的command类型 <see cref="DiffType"/>
    /// </summary>
    public List<string> Commands { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Enable)
        {
            if (string.IsNullOrEmpty(LogConnectionString))
            {
                yield return new ValidationResult("日志库连接字符串不能为空", new[] { "SqlSugarOptions.Log.LogConnectionString" });
            }

            if (Commands?.Any() != true)
            {
                yield return new ValidationResult("记录sql.command类型不能为空,可选为insert.update.delete", new[] { "SqlSugarOptions.Log.Commands" });
            }
        }
    }
}
