# 方法查询缓存

第一版通过现有 Kurisu 动态代理支持 `Task<T>` 查询结果缓存，底层复用 `ICache` 和 `ILockable`。没有新增代理框架或缓存包依赖。

## 注册与使用

```csharp
using AspectCore.Extensions.DependencyInjection;
using Kurisu.AspNetCore.Abstractions.Cache;
using Kurisu.AspNetCore.Abstractions.Cache.Aop;
using Kurisu.Extensions.Cache;
using Kurisu.Extensions.Cache.Options;

services.AddLogging();
services.Configure<RedisOptions>(configuration.GetSection("RedisOptions"));
services.AddRedis();
services.AddMethodCaching(options =>
{
    options.KeyPrefix = "shop:production"; // 不同应用和环境必须不同
    options.Policies["ProductDetail"] = new MethodCachePolicy
    {
        Expiry = TimeSpan.FromMinutes(5),
        NullExpiry = TimeSpan.FromSeconds(15),
        LockWaitTimeout = TimeSpan.FromSeconds(10),
        LockExpiry = TimeSpan.FromSeconds(6), // 持锁期间自动续租
        QueryTimeout = TimeSpan.FromSeconds(30),
        VaryByUser = false,    // 仅在同租户用户看到完全相同数据时关闭
        VaryByCulture = false // DTO 不依赖语言时关闭，便于跨语言精确失效
    };
});
services.AddScoped<IProductService, ProductService>();
// 使用 Kurisu 已有的代理容器集成。独立测试可用 services.BuildDynamicProxyProvider()。
```

```csharp
public interface IProductService
{
    [Cacheable("product-detail", Policy = "ProductDetail", KeyParameterIndex = 0)]
    Task<ProductDto?> GetAsync(long productId, CancellationToken cancellationToken = default);

    [CacheEvict("product-detail", Policy = "ProductDetail", KeyParameterIndex = 0)]
    Task UpdateAsync(long productId, UpdateProductInput input);
}
```

特性也可放在实现方法上。必须经由 DI 代理调用；第一版推荐接口代理，不保证 `new` 创建的实例或类内部自调用被拦截。

`Cacheable` / `CacheEvict` 默认 `Order = 1000`，鉴权、必要校验、事务、租户切换应先执行（更小的 Order）。使用 `[Transactional]` 的查询会绕过缓存；更新方法可组合 `[Transactional]` 和 `[CacheEvict]`。

## Key 与结果

- 默认 Key 包含前缀、区域、版本、租户、用户和角色、当前语言、完整方法身份及业务参数。跳过 `CancellationToken`，对象属性和字典 Key 按序规范化后以 SHA-256 摘要保存，不把参数明文放进 Redis Key。
- `KeyParameterIndex = -1`（查询默认值）使用方法身份和全部业务参数，包括分页、筛选条件等。
- 指定 `KeyParameterIndex` 后使用该参数的声明类型和值，不再包含方法身份。查询与失效方法必须使用相同区域、版本、业务 Key 类型与值、上下文维度；同一区域的结果结构必须一致。不要用详情 ID 代替仍受其他参数影响的查询 Key。
- `CacheEvict` 仅删除当前精确 Key，不推断列表、分页、其他用户或其他语言的缓存依赖。用户/语言相关结果需显式处理其他变体，或等待 TTL；第一版不提供区域/标签失效、`CachePut` 或跨实例 L1 缓存。
- 缓存封装对象区分缺失与 `null` / `0` / `false`。默认允许空值缓存，空值使用单独短 TTL；TTL 默认向下抖动 10%。异常和取消不缓存。
- 缓存成功完成后的 `T`；命中后恢复 `Task<T>`。拒绝非 `Task<T>` 方法、`ref/out` 参数、流、`IQueryable` 等结果。只缓存可序列化、无副作用且可重复执行的查询 DTO。
- 自定义 `ICacheKeyGenerator` 可提供业务 Key 规则；`IMethodCacheScopeContributor` 可增加权限版本等维度或要求绕过缓存。配置启动后应保持不变，结构升级时增加 `Version`。

## 分布式防击穿与故障

```text
读缓存 → 进程内按 Key 互斥 → 再读缓存
  → 单次尝试获取分布式锁，未获取则有界等待并重读缓存
  → 获得锁后再读缓存 → 查询 → 写缓存 → 释放锁
```

进程内锁引用计数回收，历史 Key 不会永久积累。分布式锁使用独立 `:load-lock` 后缀，采用现有自动续租与令牌校验释放机制。

| 情况 | 默认行为 |
| --- | --- |
| 本地互斥等待超时 | 抛出 `TimeoutException`，不无锁回源 |
| 分布式锁等待超时 | 抛出 `TimeoutException`；`BypassOnLockTimeout` 可允许在本地互斥下直查，不回填 |
| 读取缓存或获取锁失败 | 抛出异常；`BypassOnCacheFailure` 可允许在本地互斥下直查，不回填 |
| 查询失败／取消 | 原样传播，释放锁，不缓存、不重试业务查询 |
| 查询成功、写缓存失败 | 记录警告，返回查询结果 |
| 查询期间锁报告丢失 | 返回查询结果，跳过回填 |
| 释放锁失败 | 记录错误，依赖租约过期 |

等待超时涵盖本地和分布式等待。`QueryTimeout` 通过替换方法的 `CancellationToken` 协作取消查询；没有该参数或业务不响应取消时，仍会等待查询结束后释放锁，避免后台查询继续使用已释放的 DI 作用域。不要把查询超时视为强制终止线程。

同 Key 单回源保证受租约有效性约束。`Acquired` 是锁组件报告的状态，并非 Redis 原子 fencing；网络分区、续租延迟或租约失效时仍可能出现重复查询。缓存填充锁不承担业务正确性锁的职责。

## 事务与 SqlSugar

`CacheEvict` 在方法成功后调用 `ITransactionCallbackRegistry.RegisterAfterCommitAsync`：有事务时等真正提交，无事务时立即失效，回滚丢弃回调。Key 在方法执行前捕获，避免参数或上下文变化后删除错误 Key。业务异常不触发失效。

活动事务中的查询绕过共享缓存，避免缓存未提交数据。SqlSugar 注册器提供 `HasActiveTransaction`；其他适配器应实现该属性和真实提交回调，默认接口实现保守返回 `true`。未注册事务适配器时，按无事务处理；直接通过数据库连接启动、框架未跟踪的事务不受此机制管理。

`AddSqlSugar` 自动注册上下文贡献器：直接通过 `IDbTenantAccessor.GetTenantId()` 读取实际租户，加入数据源名称；忽略租户、跨租户、忽略软删除、启用数据权限及启用分表的特殊查询保守绕过。后台任务需先 `using var lifecycle = serviceProvider.InitLifecycle()`，与现有 SqlSugar 用法一致。

这是最终一致性缓存。提交后删除失败会记录日志，SqlSugar 现有回调机制会记录异常并继续，数据靠 TTL 收敛；没有事务注册器时，立即删除失败会向调用方传播。并发旧查询回填、提交后进程崩溃等窗口尚未用版本校验或 outbox 解决。

## 观测与验证

日志记录缓存读取、写入、获取锁、失效、释放锁失败。Meter 名为 `Kurisu.MethodCache`，计数器为 `kurisu.method_cache.events`，通过 `outcome` 区分 `hit`、`miss`、`load`、`bypass`、`lock_timeout`、`read_error`、`lock_error`、`write_error`、`lease_lost`、`release_error`、`evict`、`evict_error`；不以业务 Key 作为指标标签。

```bash
# 无外部 Redis：单元测试和 SQLite 事务集成测试
dotnet test test/Kurisu.Test.Cache --filter 'FullyQualifiedName~MethodCacheTests|FullyQualifiedName~MethodCacheSqlSugarTests'

# 真实 Redis：沿用 RedisOptions__ConnectionString 环境变量
dotnet test test/Kurisu.Test.Cache --filter FullyQualifiedName~MethodCacheRedisTests
```
