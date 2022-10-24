using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Kurisu.ConfigurableOptions.Attributes;

namespace Kurisu.DataAccessor
{
    /// <summary>
    /// 数据库配置
    /// </summary>
    [Configuration]
    public class DbSetting
    {
        /// <summary>
        /// 慢sql指定时间
        /// </summary>
        public int SlowSqlTime { get; set; }

        /// <summary>
        /// 默认连接字符串
        /// </summary>
        [Required(ErrorMessage = "默认连接字符串为空")]
        public string DefaultConnectionString { get; set; }

        /// <summary>
        /// 读库连接字符串
        /// </summary>
        public IEnumerable<string> ReadConnectionStrings { get; set; }

        /// <summary>
        /// sql查询超时时间
        /// <remarks>
        /// 默认30秒
        /// </remarks>
        /// </summary>
        public int Timeout { get; set; } = TimeSpan.FromSeconds(30).Seconds;

        /// <summary>
        /// 版本号
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 迁移类库
        /// </summary>
        public string MigrationsAssembly { get; set; }
    }
}