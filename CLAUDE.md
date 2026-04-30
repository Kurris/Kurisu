# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 语言与注释规范

.NET 8 WebAPI 后端框架。中文注释，英文 API 名称（类名、方法名、变量名）。**所有思考与输出均使用中文。** 所有代码、接口、配置、文档等必须包含中文注释，注释内容使用半角标点符号。

## 构建与测试

```bash
dotnet build
dotnet test
```

运行单个测试项目：
```bash
dotnet test test/Kurisu.Test.Cache/Kurisu.Test.Cache.csproj
```

运行 Analyzer 测试：
```bash
dotnet test src/Kurisu.Transaction.Analyzer/Kurisu.Transaction.Analyzer.Tests/Kurisu.Transaction.Analyzer.Tests.csproj
```

## 架构概览

### 入口模式

应用使用 `KurisuHost.Run<YourStartup>(args)` 启动，其中 `YourStartup` 继承 `DefaultKurisuStartup`（来自 `Kurisu.AspNetCore` 包）。`DefaultStartup` 按顺序注册配置、DI 扫描、路由、控制器（Newtonsoft.Json）、统一 API 结果、对象映射（Mapster）、AppModule。

### 依赖注入

- **`[DiInject]`**（`Kurisu.AspNetCore.Abstractions.DependencyInjection`）标记需要自动注册的类。属性：`Named`、`Lifetime`（默认 Scoped）、`IgnoreServiceTypes`。`DependencyInjectionHelper` 从入口程序集开始递归扫描所有引用程序集。
- **`[SkipScan]`** 排除类型。
- **`[Configuration]`**（`Kurisu.AspNetCore.Abstractions.ConfigurableOptions`）标记配置类，自动绑定 `IOptions<T>` 并对指定 section（默认为类名），调用 `ValidateDataAnnotations`。
- **`[AsApi]`** 标记动态 API 控制器，框架自动将其暴露为 Controller。

### AOP（面向切面编程）

所有 AOP 属性（`[Transactional]`、`[TryLock]` 等）均继承 `AopAttribute`（来自 `Kurisu.Aspect.Abstractions`，`AspectCore.DynamicProxy` 命名空间）。动态代理通过 `DynamicProxyServiceProviderFactory` 启用（`IHostBuilder.UseDynamicProxy()`，来自 `Kurisu.Aspect`）。

### 模块系统

`AppModule`（抽象类，实现 `IAppModule`）提供可插拔的启动生命周期：`ConfigureServices` → `Invoke` → `Configure`。属性：`Order`（默认 100）、`IsEnable`、`IsBeforeUseRouting`。非抽象子类会被自动发现并按 Order 排序。框架内置模块：`DefaultCorsModule`、`DefaultGlobalExceptionModule`、`DefaultJwtAuthenticationModule`、`DefaultSwaggerModule` 等。

### 分布式锁（Kurisu.Extensions.Cache）

`ILockable.LockAsync()` 由 `RedisCache` 实现，基于 StackExchange.Redis。`RedisLock`（内部 `ILockHandler` 实现）使用 Lua 脚本原子 compare-and-set，支持自动续期后台任务、可重入锁。锁策略通过 `IDistributedLockRetryStrategy`（重试）、`IDistributedLockTimeModeHandler`（锁时间模式，如固定过期、固定续期次数、无限续期）控制。AOP 属性：`[TryLock]`、`[TryLockFixedExpiry]`、`[TryLockLimitedRenewals]`、`[TryLockCustomHandler]`。

### 事件总线（Kurisu.Extensions.EventBus）

进程内 pub/sub，使用 `System.Threading.Channels` + 事务发件箱。`DefaultEventBus.PublishAsync<TMessage>()` 标记 `[Transactional(Propagation.Mandatory)]`，先将消息持久化到 `LocalMessage` 表，再写入 `Channel<EventMessage>`。后台服务消费 Channel、重试失败消息。

### 远程调用（Kurisu.RemoteCall）

接口标记 `[EnableRemoteClient]`，注册时通过 `ProxyGenerator.Create()` 动态代理。方法调用由 `HttpClientRemoteCallClient` 拦截，基于方法属性（`[Get]`、`[Post]`、`[Put]`、`[Delete]`、`[Patch]`、`[RequestHeader]`、`[RequestQuery]`、`[RequestBody]`、`[RequestRoute]`、`[RequestAuthorize]`、`[ResponseResult]` 等）构建 HTTP 请求。支持 `$(ConfigPath)` 语法读取 base URL。拦截器管道：参数验证 → URL 解析 → 前置处理（含授权） → 后置处理 → 错误处理。

### 数据访问（Kurisu.Extensions.SqlSugar + ContextAccessor）

SqlSugar ORM 封装。`IDatasourceManager` 通过 `ContextAccessor<NamesDbConnectionStringStack>`（连接字符串名称栈）支持多数据源切换。`DefaultSqlSugarConfigHandler` 注册全局查询过滤器：`ISoftDeleted`（软删除）、`ITenantId`（多租户），并自动填充审计字段（创建/更新用户、日期）、慢 SQL 日志。

### Analyzer（Kurisu.Transaction.Analyzer）

Roslyn 分析器，诊断 ID `KS1001`：验证 `[Transactional(Propagation = Propagation.Mandatory)]` 修饰的方法必须在已有 `[Transactional]` 修饰的调用链中被调用。编译时检查，支持显式/隐式接口实现、重写方法、递归接口层级（深度 10）。分析器嵌入 `Kurisu.AspNetCore.Abstractions` NuGet 包中，消费者自动获得。

### 项目依赖关系

```
Kurisu.AspNetCore（主元包，v0.10.6）
  ├── Kurisu.Extensions.Cache（Redis 缓存/锁）→ Kurisu.AspNetCore.Abstractions
  ├── Kurisu.Aspect（AOP 实现）→ Kurisu.Aspect.Abstractions
  └── ASP.NET Core / Serilog / Swagger / JWT / Mapster

Kurisu.AspNetCore.Abstractions（核心抽象）
  ├── Kurisu.Aspect.Abstractions（零依赖的 AOP 基础）
  └── 嵌入 Kurisu.Transaction.Analyzer（Roslyn 分析器）

Kurisu.Extensions.SqlSugar → Kurisu.AspNetCore.Abstractions + ContextAccessor + SqlSugarCore
Kurisu.Extensions.EventBus → Kurisu.Extensions.SqlSugar
Kurisu.Extensions.DataProtection.SqlSugar → Kurisu.Extensions.SqlSugar
Kurisu.RemoteCall（独立，无项目依赖，仅 Microsoft.Extensions）
```

### 关键命名空间

| 命名空间 | 用途 |
|---|---|
| `Kurisu.AspNetCore.Startup` | `KurisuHost`、`DefaultStartup` |
| `Kurisu.AspNetCore.DependencyInjection` | `DependencyInjectionHelper` |
| `Kurisu.AspNetCore.Abstractions.Cache` | `ICache`、`ILockable`、锁策略抽象 |
| `Kurisu.AspNetCore.Abstractions.DataAccess.Aop` | `[Transactional]`、数据源 AOP 属性 |
| `Kurisu.AspNetCore.Abstractions.DependencyInjection` | `[DiInject]`、`[SkipScan]` |
| `Kurisu.AspNetCore.Abstractions.ConfigurableOptions` | `[Configuration]`、`IStartupConfigure<T>` |
| `Kurisu.AspNetCore.Abstractions.Result` | `IApiResult`、`UserFriendlyException` |
| `Kurisu.AspNetCore.Abstractions.Startup` | `AppModule`、`IAppModule` |
| `AspectCore.DynamicProxy` | `AopAttribute`、`AspectContext`、`AspectDelegate` |
| `Kurisu.RemoteCall.Attributes` | `[EnableRemoteClient]`、`[Get]`、`[Post]` 等 |
