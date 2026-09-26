# 方法查询缓存

通过现有 Kurisu 动态代理提供方法查询缓存，仅支持返回 `Task<T>` 的查询方法，底层复用 `ICache` 和 `ILockable`。表达式编译复用独立的 `Kurisu.Expressions` 库。

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
        Expiry = TimeSpan.FromMinutes(30),
        LockWaitTimeout = TimeSpan.FromSeconds(10),
        LockExpiry = TimeSpan.FromSeconds(6), // 持锁期间自动续租
        QueryTimeout = TimeSpan.FromSeconds(30)
    };
});
services.AddScoped<IProductService, ProductService>();
// 使用 Kurisu 已有的代理容器集成。独立测试可用 services.BuildDynamicProxyProvider()。
```

```csharp
public interface IProductService
{
    [Cacheable("product-detail", Policy = "ProductDetail", Key = "productId")]
    Task<ProductDto?> GetAsync(long productId, CancellationToken cancellationToken = default);

    [CacheEvict("product-detail", Policy = "ProductDetail", Key = "input.Id")]
    Task UpdateAsync(UpdateProductInput input);
}
```

特性也可放在实现方法上。必须经由 DI 代理调用；第一版推荐接口代理，不保证 `new` 创建的实例或类内部自调用被拦截。

`Cacheable` 沿用 AOP 默认的 `Order = 0`，由开发者按组合场景设置执行顺序；`CacheEvict` 默认 `Order = 1000`。缓存命中会跳过后续拦截器及方法体，因此同一 AOP 链中必须执行的检查、用于缓存 Key 的租户切换应先于缓存执行。需要事务内查询绕过缓存时，应让 `[Transactional]` 先执行，使缓存能识别活动事务；更新方法可组合 `[Transactional]` 和 `[CacheEvict]`。

## Key 与结果

- 默认 Key 包含前缀、区域、版本、租户、完整方法身份及业务参数，不包含当前用户、角色和语言。跳过 `CancellationToken`，对象属性和字典 Key 按序规范化后以 SHA-256 摘要保存，不把参数明文放进 Redis Key。
- 同租户用户在其他 Key 维度相同时共享结果。仅对允许共享且能接受缓存延迟的查询使用 `Cacheable`；依赖当前用户或角色的结果、要求实时的数据不应标记此特性。
- 查询未指定 `Key` 时使用方法身份和全部业务参数。
- 指定 `Key` 表达式后使用求值结果的运行时类型和值，不再包含方法身份。查询与失效方法必须使用相同区域、版本、业务 Key 类型与值、上下文维度；同一区域的结果结构必须一致。不要用详情 ID 代替仍受其他参数影响的查询 Key。
- `CacheEvict` 必须且只能指定 `Key`（单个值）或 `Keys`（集合）；集合按单个值生成 Key 并去重。仅删除显式指定的 Key，不推断列表、分页或其他查询的缓存依赖。其他缓存变体需显式失效，或等待 TTL；第一版不提供区域/标签失效、`CachePut` 或跨实例 L1 缓存。
- 缓存封装对象区分缺失与 `null` / `0` / `false`。非空结果默认有效期为 30 分钟；空值始终缓存，基准有效期固定为 30 秒，不提供空值开关或有效期配置。两种 TTL 均由内部固定按最多 10% 向下随机浮动，空值实际有效期约为 27～30 秒。异常和取消不缓存。
- 缓存成功完成后的 `T`；命中后恢复 `Task<T>`。拒绝非 `Task<T>` 方法和 `ref/out` 参数。不支持返回 `Pagination<T>` 及其派生类型（包括非泛型 `Pagination`），调用时在读取缓存和执行业务方法前抛出 `NotSupportedException`。结果应为可序列化的查询 DTO，方法应无副作用且可重复执行；其他结果类型不做预先检查，由实际执行和序列化过程处理。
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
| 分布式锁等待超时 | 抛出 `TimeoutException`，不直接回源查询 |
| 读取缓存或获取锁失败 | 记录错误并抛出异常，不回源查询 |
| 查询失败／取消 | 原样传播，释放锁，不缓存、不重试业务查询 |
| 查询成功、写缓存失败 | 记录警告，返回查询结果 |
| 查询期间锁报告丢失 | 返回查询结果，跳过回填 |
| 释放锁失败 | 记录错误，依赖租约过期 |

等待超时涵盖本地和分布式等待。`QueryTimeout` 通过替换方法的 `CancellationToken` 协作取消查询；没有该参数或业务不响应取消时，仍会等待查询结束后释放锁，避免后台查询继续使用已释放的 DI 作用域。不要把查询超时视为强制终止线程。

同 Key 单回源保证受租约有效性约束。`Acquired` 是锁组件报告的状态，并非 Redis 原子 fencing；网络分区、续租延迟或租约失效时仍可能出现重复查询。缓存填充锁不承担业务正确性锁的职责。

## 事务与 SqlSugar

`CacheEvict` 在方法成功后调用 `ITransactionCallbackRegistry.RegisterAfterCommitAsync`：有事务时等真正提交，无事务时立即失效，回滚丢弃回调。`Condition`、Key 和上下文在方法执行前捕获，批量结果提前物化，避免 Save 回填 ID 或修改集合后删除错误 Key。条件为 false 或批量结果为空时仍执行业务方法，但不注册回调；非空批量只注册一次提交回调。业务异常不触发失效。

活动事务中的查询绕过共享缓存，避免缓存未提交数据。SqlSugar 注册器提供 `HasActiveTransaction`；其他适配器应实现该属性和真实提交回调，默认接口实现保守返回 `true`。未注册事务适配器时，按无事务处理；直接通过数据库连接启动、框架未跟踪的事务不受此机制管理。

缓存层直接通过公共抽象 `IDbTenantAccessor.GetTenantId()` 读取最终租户（包括 `UseTenant` 覆盖）；未注册该接口时使用 `ICurrentTenant`，再回退到 `ICurrentUser`。已注册访问器返回 null 时，以该结果为准，不回退到身份租户。数据源固定，不加入 Key。跨租户、忽略租户或软删除过滤、启用数据权限及分表等特殊查询不应使用 `Cacheable`，由开发者保证使用边界；框架不检测这些状态或自动绕过缓存。后台任务需先 `using var lifecycle = serviceProvider.InitLifecycle()`，与现有 SqlSugar 用法一致。

这是最终一致性缓存。提交后删除失败会记录日志，SqlSugar 现有回调机制会记录异常并继续，数据靠 TTL 收敛；没有事务注册器时，立即删除失败会向调用方传播。并发旧查询回填、提交后进程崩溃等窗口尚未用版本校验或 outbox 解决。

## 日志与验证

缓存读取、写入、获取锁、失效及释放锁失败时记录日志；查询期间丢失锁租约时记录警告。

```bash
# 无外部 Redis：单元测试和 SQLite 事务集成测试
dotnet test test/Kurisu.Test.Cache --filter 'FullyQualifiedName~MethodCacheTests|FullyQualifiedName~MethodCacheSqlSugarTests'

# 真实 Redis：沿用 RedisOptions__ConnectionString 环境变量
dotnet test test/Kurisu.Test.Cache --filter FullyQualifiedName~MethodCacheRedisTests
```

## 表达式与锁迁移

`AddMethodCaching` 和 `AddRedis` 均注册单例 `ExpressionCompiler`、`MethodExpressionEvaluator`，按方法签名绑定参数并复用委托。
表达式统一使用 `AspectContext.ServiceMethod` 的参数名：接口代理按接口参数名填写，类代理按服务方法参数名填写，与特性声明位置无关。接口参数叫 `id`、实现参数叫 `value` 时，即使特性写在实现上，也使用 `Key = "id"`。业务参数类型和成员需可公开访问。编译错误会在业务执行前抛出。

```csharp
[CacheEvict("product-detail", Key = "input.Id", Condition = "input.Id > 0")]
Task SaveAsync(ProductInput input);

[CacheEvict("product-detail", Keys = "inputs.Where(x => x.Id > 0).Select(x => x.Id)")]
Task SaveBatchAsync(List<ProductInput> inputs);

[TryLock("product", "正在处理", Key = "input.Id")]
Task UpdateAsync(ProductInput input);

[TryLock("product", "正在处理", Keys = "inputs.Select(x => x.Id)")]
Task UpdateBatchAsync(List<ProductInput> inputs);
```

`Id > 0` 仅为业务条件示例，应按业务实际规则区分新增和更新。新增如果影响已有的空结果缓存，也需要失效对应 Key。
`KeyParameterIndex`、`ITryLockKey`、`ITryLockKeys` 已移除。DTO 无需实现取 Key 接口；消息锁可使用 `Key = "message.Code"`。
`TryLock` 必须且只能指定 `Key` 或 `Keys`，结果以固定文化格式转为字符串；多锁按 Key 去重和排序后获取、逆序释放，空集合报错。
直接使用 `MultiLock.AcquireAsync` 时传入字符串 Key 集合。自定义锁提供者若不调用上述注册方法，需自行将两个表达式组件注册为单例。

这是 Key API 的不兼容调整。显式表达式使用运行时类型生成 Key，发布时可递增策略 `Version` 隔离旧 Key，避免新旧实例滚动部署期间失效规则不一致。

`IDbTenantAccessor` 已移至公共抽象层，命名空间为 `Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context`。缓存层不依赖 SqlSugar 实现。
