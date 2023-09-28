using Kurisu.DataAccess.Sharding;
using Kurisu.Grpc;
using Kurisu.EFSharding;
using Kurisu.EFSharding.Dynamicdatasources;
using Kurisu.Startup;
using Kurisu.Test.WebApi_A.Routes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Kurisu.DataAccess.Functions.Default;
using Kurisu.DataAccess.Internal;
using Kurisu.DataAccess.Functions.Default.Abstractions;

namespace Kurisu.Test.WebApi_A;

public class Startup : DefaultKurisuStartup
{
    public Startup(IConfiguration configuration) : base(configuration)
    {
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddGrpc(options =>
        {
            options.Interceptors.Add<ServiceLoggerInterceptor>();
            options.EnableDetailedErrors = true;
        });

        services.AddSingleton<IMemoryCache>(sp => new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 102400
        }));

        services.AddKurisuDatabaseAccessor();
        //services.AddShardingDbContext<DefaultShardingDbContext>()
        //    .UseRouteConfig((sp, o) =>
        //    {
        //        o.AddShardingTableRoute<TestTableMod>();
        //        o.AddShardingDatasourceRoute<TestDatasource>();
        //    }).UseConfig((sp, o) =>
        //    {
        //        o.UseEntityFrameworkCoreProxies = true;
        //        o.CacheModelLockConcurrencyLevel = 1024;
        //        o.CacheEntrySize = 1;
        //        o.CacheModelLockObjectSeconds = 10;

        //        o.UseShardingQuery((conStr, builder) => { builder.UseMySql(conStr, new MySqlServerVersion(MySqlServerVersion.LatestSupportedServerVersion)); });
        //        o.UseShardingTransaction((connection, builder) => { builder.UseMySql(connection, new MySqlServerVersion(MySqlServerVersion.LatestSupportedServerVersion)); });
        //        o.AddDatasource(_ => new List<DatasourceUnit>
        //        {
        //            new() {IsDefault = true, Name = "ds0", ConnectionString = "server=isawesome.cn;port=3306;userid=root;password=@zxc111;database=demo0;Charset=utf8mb4;"},
        //            new() {IsDefault = false, Name = "ds1", ConnectionString = "server=isawesome.cn;port=3306;userid=root;password=@zxc111;database=demo1;Charset=utf8mb4;"},
        //            new() {IsDefault = false, Name = "ds2", ConnectionString = "server=isawesome.cn;port=3306;userid=root;password=@zxc111;database=demo2;Charset=utf8mb4;"}
        //        });
        //    })
        //    .ReplaceService<IModelCacheLockerProvider, DicModelCacheLockerProvider>()
        //    .AddShardingCore();
    }

    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        //app.ApplicationServices.UseAutoTryCompensateTable().GetAwaiter().GetResult();
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DefaultAppDbContext<IDbWrite>>();
            db.RunDynamicMigrationAsync().Wait();
        }

        // IdentityModelEventSource.ShowPII = true;

        base.Configure(app, env);
        app.UseEndpoints(builder => builder.MapGrpcServices());
    }
}