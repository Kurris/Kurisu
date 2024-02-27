using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kurisu.AspNetCore.Utils.Extensions;

public static class SnowFlakeApplicationBuilderExtensions
{
    public static void UseSnowFlakeInit(this IApplicationBuilder app)
    {
        var key = "SnowFlake";
        var range = Enumerable.Range(1, 200);

        var redis = app.ApplicationServices.GetService<RedisCache>();
        var lifetime = app.ApplicationServices.GetService<IHostApplicationLifetime>();

        var ids = redis.ListRange(key);
        foreach (var item in range)
        {
            if (!ids.Contains(item))
            {
                redis.ListLeftPush(key, item);
                SnowFlakeHelper.Initialize(1, item);
                lifetime.ApplicationStopping.Register(() => redis.ListRemove(key, item.ToString()));
                return;
            }
        }

        throw new Exception("超出预设负载");
    }
}
