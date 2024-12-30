using System.ComponentModel.DataAnnotations;
using Kurisu.AspNetCore.ConfigurableOptions.Attributes;

namespace Kurisu.AspNetCore.Cache.Options;

/// <summary>
/// redis配置
/// </summary>
[Configuration]
public class RedisOptions
{
    /// <summary>
    /// 连接字符串
    /// </summary>
    [Required(ErrorMessage = "redis连接字符串不能为空")]
    public string ConnectionString { get; set; }
}