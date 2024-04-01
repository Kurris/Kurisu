﻿using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kurisu.AspNetCore.Utils.Extensions;

/// <summary>
/// 雪花id启用扩展
/// </summary>
public static class SnowFlakeApplicationBuilderExtensions
{
    /// <summary>
    /// 启用雪花id初始化
    /// </summary>
    /// <param name="app"></param>
    /// <exception cref="Exception"></exception>
    public static void UseSnowFlakeDistributedInitialize(this IApplicationBuilder app)
    {
        var key = "snowflake";
        var range = Enumerable.Range(1, 32);

        var cache = app.ApplicationServices.GetService<RedisCache>();
        var logger = app.ApplicationServices.GetService<ILogger<SnowFlakeHelper>>();
        var lifetime = app.ApplicationServices.GetService<IHostApplicationLifetime>();
        using (var handler = cache.Lock("snowflake-initialize", TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(500), 6))
        {
            if (handler.Acquired)
            {
                var ids = cache.ListRange(key);
                foreach (var item in range)
                {
                    if (!ids.Contains(item))
                    {
                        var pr = cache.ListLeftPush(key, item);
                        SnowFlakeHelper.Initialize(1, item);
                        logger.LogInformation("雪花workerId初始化完成:{workerId}.结果:{result}", item, pr);
                        lifetime.ApplicationStopping.Register(() =>
                        {
                            var rr = cache.ListRemove(key, item.ToString());
                            logger.LogInformation("雪花workerId移除:{workerId}.结果:{result}", item, rr);
                        });
                        return;
                    }
                }
            }
        }

        throw new Exception("雪花workerId初始化失败");
    }
}
