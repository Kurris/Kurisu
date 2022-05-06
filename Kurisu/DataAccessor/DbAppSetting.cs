using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Kurisu.ConfigurableOptions.Abstractions;
using Kurisu.ConfigurableOptions.Attributes;
using Microsoft.Extensions.Configuration;

namespace Kurisu.DataAccessor
{
    [Configuration]
    public class DbAppSetting : IPostConfigure<DbAppSetting>
    {
        /// <summary>
        /// 慢sql规定时间
        /// </summary>
        public int SlowSqlTime { get; set; }

        /// <summary>
        /// 默认连接字符串
        /// </summary>
        public string DefaultConnectionString { get; set; }

        /// <summary>
        /// 读库连接字符串
        /// </summary>
        public List<string> ReadConnectionStrings { get; set; }

        /// <summary>
        /// sql查询超时时间
        /// </summary>
        [Required]
        public int Timeout { get; set; }

        /// <summary>
        /// 版本号
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 迁移类库
        /// </summary>
        public string MigrationsAssembly { get; set; }

        public void PostConfigure(IConfiguration configuration, DbAppSetting options)
        {
            options.ReadConnectionStrings ??= new List<string>();
        }
    }
}