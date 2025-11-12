using System.ComponentModel.DataAnnotations;
using Kurisu.AspNetCore.Abstractions.ConfigurableOptions;

namespace Kurisu.Extensions.SqlSugar.Options;

/// <summary>
/// SqlSugar配置options
/// </summary>
[Configuration]
public class SqlSugarOptions
{
    /// <summary>
    /// 主库连接字符串(默认)
    /// </summary>
    [Required(ErrorMessage = "默认连接字符串不能为空")]
    public string DefaultConnectionString { get; set; }

    /// <summary>
    /// 是否启用sql日志
    /// </summary>
    public bool EnableSqlLog { get; set; }

    /// <summary>
    /// sql执行超时时间 unit:second
    /// </summary>
    [Range(1, 300)]
    public int Timeout { get; set; }

    /// <summary>
    /// 慢sql判定时间 unit:second
    /// </summary>
    [Range(1, 300)]
    public int SlowSqlTime { get; set; }
}