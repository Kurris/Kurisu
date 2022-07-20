# 原生二次封装框架
### 如何使用
- **Program** 启动项修改为框架执行
```csharp
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder(args).RunKurisuAsync<Startup>();
        }
    }
```
- **Startup** 继承`DefaultKurisuStartup`使用默认的配置, override修饰的方法按需重写
```csharp 
    public class Startup : DefaultKurisuStartup
    {
        public Startup(IConfiguration configuration) : base(configuration)
        {
        }
        
        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
        }

        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            base.Configure(app, env);
        }
    }
```
