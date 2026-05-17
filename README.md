# Kurisu

ASP.NET Core 二次封装框架，提供约定式启动、自动依赖注入、模块化装配、AOP 和一系列开箱即用的企业级扩展。

[![NuGet](https://img.shields.io/badge/nuget-v0.10.6-blue)](https://www.nuget.org/packages/Kurisu.AspNetCore)

## 快速开始

**Program.cs**

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        KurisuHost.Run<Startup>(args);
    }
}
```

**Startup.cs**

```csharp
public class Startup : DefaultStartup
{
    public Startup(IConfiguration configuration) : base(configuration) { }

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

## 核心特性

### 自动依赖注入

标注 `[DiInject]` 的类自动注册到 DI 容器，框架启动时扫描所有 project 程序集。

```csharp
[DiInject(Lifetime = ServiceLifetime.Singleton)]
public class UserService : IUserService { }

[DiInject("sms")]  // 命名服务（keyed service）
public class SmsSender : IMessageSender { }
```

- `Lifetime` — 生命周期，默认 `Scoped`
- `Named` — 命名服务，解析时通过 `INamedResolver.GetService<T>(name)` 获取
- `IgnoreServiceTypes` — 排除不需要注册的接口或基类
- `[SkipScan]` — 标记在类上可跳过扫描

### 模块化装配

继承 `AppModule` 定义可插拔模块，框架自动发现并按 `Order` 排序执行。

```csharp
public class MyModule : AppModule
{
    public override string Name => "MyModule";
    public override int Order => 100;
    public override bool IsEnable => true;

    public override void ConfigureServices(IServiceCollection services) { }
    public override void Invoke(IServiceProvider serviceProvider) { }
    public override void Configure(IApplicationBuilder app) { }
}
```

内置模块：

| 模块 | 说明 |
|------|------|
| `DefaultGlobalExceptionModule` | 全局异常处理中间件 |
| `DefaultSwaggerModule` | Swagger / Swashbuckle |
| `DefaultCorsModule` | CORS 策略 |
| `DefaultHealthCheckModule` | 健康检查 `/healthz` |
| `DefaultJwtAuthenticationModule` | JWT Bearer 认证 |
| `DefaultOAuth2AuthenticationModule` | OAuth2 / OIDC 认证 |
| `MultiLanguageModule` | 多语言支持 |

### 配置自动绑定

```csharp
[Configuration("Jwt")]            // 绑定 IConfiguration 的 "Jwt" 节
public class JwtOptions : IStartupConfigure<JwtOptions>
{
    public string SecretKey { get; set; }
    public string Issuer { get; set; }

    public void StartupConfigure(JwtOptions value)
    {
        // options 绑定后回调，可做校验或二次处理
    }
}
```

通过 `services.AddConfiguration(configuration)` 自动扫描所有 `[Configuration]` 类并绑定。

### 统一返回结果

控制器自动包装返回值，无需手动构造响应体。

```json
{
  "code": 200,
  "msg": "操作成功",
  "data": { }
}
```

### AOP 支持

基于 AspectCore 的透明代理，提供声明式横切关注点。

```csharp
[TryLock("createOrder", "订单处理中，请稍后重试")]
[Transactional]
public void CreateOrder(OrderDto dto) { }
```

内置拦截器：`[Transactional]`、`[TryLock]`、`[Datasource]`、`[IgnoreTenant]` 等。

### 远程调用

类似 Feign 的声明式 HTTP 客户端，基于接口 + 属性声明。

```csharp
[EnableRemoteClient("https://api.example.com")]
public interface IUserApi
{
    [Get("/users/{id}")]
    Task<UserDto> GetUserAsync([RequestRoute] int id);

    [Post("/users")]
    Task CreateUserAsync([RequestBody] CreateUserDto dto);
}

// 注册
services.AddRemoteCall(typeof(IUserApi));
```

## 扩展包

| 包名 | 说明 |
|------|------|
| `Kurisu.Extensions.Cache` | Redis 缓存与分布式锁 |
| `Kurisu.Extensions.SqlSugar` | SqlSugar ORM 集成（多租户、软删除、分表、事务传播） |
| `Kurisu.Extensions.EventBus` | 进程内事件总线（Channel 实现） |
| `Kurisu.Extensions.ContextAccessor` | 泛型 `AsyncLocal<T>` 上下文访问器 |
| `Kurisu.RemoteCall` | 声明式 HTTP 客户端 |
| `Kurisu.Aspect` | AOP 动态代理 |
| `Kurisu.Extensions.DataProtection.Redis` | Redis 数据保护密钥存储 |
| `Kurisu.Extensions.DataProtection.SqlSugar` | SqlSugar 数据保护密钥存储 |

## 启动流程

```
KurisuHost.Run<Startup>(args)
  └─ DefaultStartup.ConfigureServices()
       ├─ AddConfiguration()        配置自动绑定
       ├─ AddDependencyInjection()  自动 DI 扫描
       ├─ AddControllers()          MVC 配置
       ├─ AddUnifyResult()          统一返回结果
       └─ AddAppModules()           模块 ConfigureServices
  └─ DefaultStartup.Configure()
       ├─ UseAppPacks(beforeRouting)   异常处理等
       ├─ UseRouting()
       ├─ UseAppPacks(afterRouting)    CORS、认证、Swagger 等
       └─ UseEndpoints()
```

## License

MIT
