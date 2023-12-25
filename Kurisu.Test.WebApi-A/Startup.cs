using System.Reflection;
using Kurisu.AspNetCore.Grpc;
using Kurisu.AspNetCore.Grpc.Interceptors;
using Kurisu.AspNetCore.Startup;
using Kurisu.SqlSugar.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;

namespace Kurisu.Test.WebApi_A;

public class Startup : DefaultStartup
{
    public Startup(IConfiguration configuration) : base(configuration)
    {
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddHttpContextAccessor();
        services.AddGrpc(options =>
        {
            options.Interceptors.Add<ServiceLoggerInterceptor>();
            options.EnableDetailedErrors = true;
        });

        services.AddSingleton<IMemoryCache>(sp => new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 102400
        }));

        services.AddSqlSugar();

        var m = typeof(ITryTestService).GetMethods().Where(x => x.GetCustomAttribute(typeof(LogAttribute), false) != null);

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

        // var t = new Entity.Test()
        // {
        //     Id = 1,
        //     Name = "ligy"
        // };
        //
        // var u = new
        // {
        //     Name = "shx"
        // };
        //
        // u.Adapt(t);
    }

    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        //app.ApplicationServices.UseAutoTryCompensateTable().GetAwaiter().GetResult();
        //using (var scope = app.ApplicationServices.CreateScope())
        //{
        //    var db = scope.ServiceProvider.GetRequiredService<DefaultAppDbContext<IDbWrite>>();
        //    db.RunDynamicMigrationAsync().GetAwaiter().GetResult();
        //}

        // IdentityModelEventSource.ShowPII = true;

        base.Configure(app, env);

        string contentRoot = Directory.GetCurrentDirectory();
        string folder = Path.Combine(contentRoot, "files");
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        IFileProvider fileProvider = new PhysicalFileProvider(folder);
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = fileProvider,
            RequestPath = "/files"
        })
            .UseDirectoryBrowser(new DirectoryBrowserOptions
            {
                FileProvider = fileProvider,
                RequestPath = "/files"
            });

        app.UseEndpoints(builder => builder.MapGrpcServices());
    }
}