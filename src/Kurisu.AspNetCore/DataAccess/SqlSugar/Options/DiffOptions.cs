using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Kurisu.AspNetCore.ConfigurableOptions.Attributes;
using SqlSugar;

namespace Kurisu.AspNetCore.DataAccess.SqlSugar.Options;

/// <summary>
/// 差异处理options
/// </summary>
[Configuration("SqlSugarOptions:Diff")]
public class DiffOptions : IValidatableObject
{
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable { get; set; }

    /// <summary>
    /// 日志数据库连接字符串
    /// </summary>
    public string LogConnectionString { get; set; }

    /// <summary>
    /// 日志记录sql的command类型 <see cref="DiffType"/>
    /// </summary>
    public List<string> Commands { get; set; }

    /// <summary>
    /// 验证参数
    /// </summary>
    /// <param name="validationContext"></param>
    /// <returns></returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enable) yield break;

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