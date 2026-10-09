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

直接引用官方 AspectCore 3.0.0, 通过 `.UseDynamicProxy()` 在微软原生 DI 上启用透明代理。Kurisu 的切面基类为 `Kurisu.AspNetCore.Abstractions.Aop.AopAttribute`, 继承官方 `AbstractInterceptorAttribute`。

```csharp
[TryLock("createOrder", "订单处理中，请稍后重试")]
[Transactional]
public virtual void CreateOrder(OrderDto dto) { }
```

内置拦截器：`[Transactional]`、`[TryLock]`、`[Datasource]`、`[IgnoreTenant]` 等。可选的 [FluentValidation 扩展](src/Kurisu.Extensions.FluentValidation/README.md) 允许模型实现 `Kurisu.Extensions.FluentValidation.Abstractions.IAopValidatableObject` 并在 `CreateValidator` 中定义规则。宿主调用 `AddParameterValidation()` 后，扩展的方法拦截器自动异步验证这些模型，无需逐个标记参数，并在默认事务切面之前执行。只有声明为可验证模型类型的方法参数会自动触发验证, 普通服务不因验证功能而被代理。

自定义切面需引用 `Kurisu.AspNetCore.Abstractions.Aop`。原 `Kurisu.Aspect` 与 `Kurisu.Aspect.Abstractions` 项目已移除, 消费方升级后需要重新编译。

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
| `Kurisu.Extensions.EventBus` | 本地持久化事件总线（事务内发布、Channel 调度、租约、重试与死信） |
| `Kurisu.Extensions.FluentValidation` | 模型接口自动触发异步参数 AOP，支持模型内规则及宿主字段错误响应 |
| `Kurisu.Extensions.ContextAccessor` | 泛型 `AsyncLocal<T>` 上下文访问器 |
| `Kurisu.RemoteCall` | 声明式 HTTP 客户端 |
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

## 测试环境变量

运行相关测试前设置以下环境变量：

| 环境变量 | 用途 |
|------|------|
| `DbOptions__DefaultConnectionString` | 默认数据库连接 |
| `DbOptions__AdditionalConnectionStrings__SecondConnectionString` | 多数据源测试的第二个数据库连接 |
| `RedisOptions__ConnectionString` | Redis 测试连接 |

## 电商微服务用例

[Kurisu.Shop](sample/Kurisu.Shop/README.md) 提供 .NET 8、YARP 网关与 SQLite 的本地电商微服务用例，包含商城、管理后台、客户会员、营销活动、统一计价、地址与商品回收站，以及下单、库存、模拟支付和取消流程。购物车、营销与统一计价作为 Ordering 内部模块，会员由 Identity 提供。购物车默认使用后端内存缓存，可切换框架 Redis。用例使用框架表列配置、基础实体审计、数据访问与 CodeFirst、事务和参数 AOP、Mapster、动态 API、数据保护、分页及默认响应和 Swagger。

服务间消息复用 `Kurisu.Extensions.EventBus` 的 `IEventBus / LocalMessage / IEventMessageHandler<T>`，每种业务事件直接对应一个强类型框架处理器。业务数据与事件在同一事务中持久化，再通过 HTTP 桥接和 Inbox 去重实现协作，无需第三方消息中间件。SQLite 的 `long` 自增主键通过用例自己的 `ConfigureExternalServices` 替换适配，不修改框架默认配置。各业务项目以 `.Service` 结尾，可通过 PowerShell 脚本启动，并运行框架接入、HTTP 回归及完整业务流程验证。

## License

[MIT](LICENSE)
