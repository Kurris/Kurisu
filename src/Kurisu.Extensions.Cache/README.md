# 方法查询缓存

通过现有 Kurisu 动态代理提供方法查询缓存，仅支持返回 `Task<T>` 的查询方法，底层复用 `ICache` 和 `ILockable`。表达式编译复用独立的 `Kurisu.Expressions` 库。

## 默认分布式锁配置

`AddRedis()` 和 `AddMethodCaching()` 会注册 `IDistributedLockOptionsFactory`。
使用其他锁提供者时，可单独调用 `services.AddDistributedLockOptions()`，不需要 Redis。

```csharp
// 注入 IDistributedLockOptionsFactory 和 ILockable 后使用。
var options = optionsFactory.CreateDefault();
await using var handler = await lockable.LockAsync(lockKey, options, cancellationToken);
```

默认工厂每次创建独立配置和策略实例：6 秒过期、自动无限续期、最多尝试获取 3 次。
应用可实现 `IDistributedLockOptionsFactory` 并注册替代实现：

```csharp
services.AddSingleton<IDistributedLockOptionsFactory, ApplicationLockOptionsFactory>();
services.AddRedis();
```

默认注册使用 `TryAddSingleton`，不会覆盖预先注册的应用工厂；在框架注册后注册应用工厂也可替换默认服务。
自定义工厂应每次返回独立配置，避免调用方修改配置影响其他请求。
`TryLock` 未显式指定策略时使用该工厂。只覆盖显式设置的时间参数和 `RetryCount`，其余使用工厂默认配置。
`MaxRenewalCount` 统一控制续期：`LockRenewal.Disabled` 不续期、正数有限续期、`LockRenewal.Unlimited` 无限续期；未设置时沿用工厂默认配置。显式过期时间必须大于 0，续期次数不能小于 `-1`。

```csharp
// 使用 DI 工厂默认配置。
[TryLock("save", "处理中", Key = "id")]
// 固定过期，不续期。
[TryLock("save", "处理中", Key = "id", ExpirySeconds = 10, MaxRenewalCount = LockRenewal.Disabled)]
// 无限续期。
[TryLock("save", "处理中", Key = "id", MaxRenewalCount = LockRenewal.Unlimited)]
// 最多续期 3 次。
[TryLock("save", "处理中", Key = "id", ExpirySeconds = 10, MaxRenewalCount = 3)]
```

方法缓存的加载锁仍显式使用配置的过期时间和无重试策略，由缓存轮询控制等待；手动传入的锁配置也不会被工厂覆盖。
需要替换锁获取、续期或释放的实现时，可另行注册自定义 `ILockable`。

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
    options.Default.Expiry = TimeSpan.FromMinutes(10); // 未指定或未命中命名策略时兜底
});
services.AddScoped<IProductService, ProductService>();
// 使用 Kurisu 已有的代理容器集成。独立测试可用 services.BuildDynamicProxyProvider()。
```

`Default` 属性仅用于兜底，`Policies` 按名称匹配且没有保留名称。显式设置 `Policy = "Default"` 时，优先查找 `Policies["Default"]`；未配置该名称时才使用兜底策略。

```csharp
public interface IProductService
{
    [Cacheable("product-detail")]
    Task<ProductDto?> GetAsync(long productId, CancellationToken cancellationToken = default);

    [CacheEvict("product-detail")]
    Task UpdateAsync(long productId);
}
```

默认取首个方法参数作为 Key，查询与更新无须写表达式。使用 DTO 更新或批量更新时再显式指定：

```csharp
public interface IProductWriter
{
    [CacheEvict("product-detail", Key = "input.Id")]
    Task UpdateAsync(UpdateProductInput input);

    [CacheEvict("product-detail", Keys = "productIds")]
    Task DeleteAsync(long[] productIds);
}
```

多参数查询需显式组合所有影响结果的业务标识；无参数方法可显式指定常量 Key：

```csharp
[Cacheable("product-stock", Key = "$\"{productId}:{warehouseId}\"")]
Task<StockDto?> GetStockAsync(long productId, long warehouseId);

[Cacheable("settings", Key = "\"all\"")]
Task<SettingsDto> GetSettingsAsync();
```

只有需要特殊配置时才使用命名策略：

```csharp
services.AddMethodCaching(options =>
{
    options.Policies["Short"] = new MethodCachePolicy { Expiry = TimeSpan.FromSeconds(30) };
});

// 在对应服务接口中声明
[Cacheable("product-prices", Policy = "Short")]
Task<decimal> GetPriceAsync(long productId);
```

特性也可放在实现方法上。必须经由 DI 代理调用；第一版推荐接口代理，不保证 `new` 创建的实例或类内部自调用被拦截。

`Cacheable` 沿用 AOP 默认的 `Order = 0`，由开发者按组合场景设置执行顺序；`CacheEvict` 默认 `Order = 1000`。缓存命中会跳过后续拦截器及方法体，因此同一 AOP 链中必须执行的检查、用于缓存 Key 的租户切换应先于缓存执行。需要事务内查询绕过缓存时，应让 `[Transactional]` 先执行，使缓存能识别活动事务；更新方法的事务边界由开发者定义，`CacheEvict` 不等待外层事务提交。

## Key 与结果

- Key 包含前缀、区域、版本、租户及业务 Key，不绑定业务 Key 的 CLR 类型，不包含方法身份、当前用户、角色和语言。业务 Key 仅接受 `string`、`int`、`long`，整数转为固定文化格式字符串；`int 7`、`long 7`、字符串 `"7"` 对应相同 Key。null、DTO、集合或其他类型会在业务执行前报配置错误，请通过表达式显式选取或转换为字符串。
- Key 直接拼接，不进行 JSON 序列化或摘要计算。区域、版本、上下文名称和值、业务 Key 使用 URI 转义，避免 `:`、`=`、`%` 等字符产生歧义。例如 `shop:production:data:s:products:1:scope:1:tenant=a:key:7`。上下文维度按名称排序并包含数量，仅支持 `string`、`int`、`long` 或 null；上下文中的 null 与空字符串分别编码，复杂维度需由贡献器先转换为业务字符串。
- 同租户用户在其他 Key 维度相同时共享结果。仅对允许共享且能接受缓存延迟的查询使用 `Cacheable`；依赖当前用户或角色的结果、要求实时的数据不应标记此特性。
- 查询未指定 `Key`、失效未指定 `Key` 或 `Keys` 时，固定取首个方法参数，不跳过首位 `CancellationToken`；其余参数及参数名不参与 Key。无参数方法必须显式指定 Key。多参数若共同影响结果，使用表达式显式组合成字符串，框架不自动组合或序列化参数。
- 指定 `Key` 表达式后使用求值结果的字符串值；首参数默认 Key 与直接返回该参数的显式 Key 一致。查询与失效方法必须使用相同区域、版本、业务 Key 及上下文维度；同一区域的结果结构和查询语义必须一致。不同方法若这些维度相同会共享缓存，不同结果应使用不同区域。不要用详情 ID 代替仍受其他参数影响的查询 Key。
- `CacheEvict.Key` 删除单个条目，`Keys` 将集合逐项生成 Key 并去重，每项仅接受 `string`、`int`、`long`，不能同时指定。未指定时按首参数删除一个条目，不表示清空区域。不推断列表、分页或其他查询的缓存依赖。其他缓存变体需显式失效，或等待 TTL；第一版不提供区域/标签失效、`CachePut` 或跨实例 L1 缓存。
- 缓存封装对象区分缺失与 `null` / `0` / `false`。非空结果默认有效期为 30 分钟；空值始终缓存，基准有效期固定为 30 秒，不提供空值开关或有效期配置。两种 TTL 均由内部固定按最多 10% 向下随机浮动，空值实际有效期约为 27～30 秒。异常和取消不缓存。
- 缓存成功完成后的 `T`；命中后恢复 `Task<T>`。拒绝非 `Task<T>` 方法和 `ref/out` 参数。不支持返回 `Pagination<T>` 及其派生类型（包括非泛型 `Pagination`），调用时在读取缓存和执行业务方法前抛出 `NotSupportedException`。结果应为可序列化的查询 DTO，方法应无副作用且可重复执行；其他结果类型不做预先检查，由实际执行和序列化过程处理。
- 自定义 `ICacheKeyGenerator` 可提供业务 Key 规则；`IMethodCacheScopeContributor` 可增加权限版本等维度或要求绕过缓存。配置启动后应保持不变，结构升级时增加 `Version`。

此次统一规则改变了既有 Key 格式，旧缓存不会被复用，按原 TTL 过期。

## 分布式防击穿与故障

```text
读缓存 → 进程内按 Key 互斥 → 再读缓存
  → 单次尝试获取分布式锁，未获取则有界等待并重读缓存
  → 获得锁后再读缓存 → 查询 → 写缓存 → 释放锁
```

进程内锁引用计数回收，历史 Key 不会永久积累。分布式锁使用独立 `:load-lock` 后缀，采用现有自动续租与令牌校验释放机制。

| 情况                 | 默认行为                                 |
|----------------------|------------------------------------------|
| 本地互斥等待超时     | 抛出 `TimeoutException`，不无锁回源      |
| 分布式锁等待超时     | 抛出 `TimeoutException`，不直接回源查询  |
| 读取缓存或获取锁失败 | 记录错误并抛出异常，不回源查询           |
| 查询失败／取消       | 原样传播，释放锁，不缓存、不重试业务查询 |
| 查询成功、写缓存失败 | 记录警告，返回查询结果                   |
| 查询期间锁报告丢失   | 返回查询结果，跳过回填                   |
| 释放锁失败           | 记录错误，依赖租约过期                   |

等待超时涵盖本地和分布式等待。`QueryTimeout` 通过替换方法的 `CancellationToken` 协作取消查询；没有该参数或业务不响应取消时，仍会等待查询结束后释放锁，避免后台查询继续使用已释放的 DI 作用域。不要把查询超时视为强制终止线程。

同 Key 单回源保证受租约有效性约束。`Acquired` 是锁组件报告的状态，并非 Redis 原子 fencing；网络分区、续租延迟或租约失效时仍可能出现重复查询。缓存填充锁不承担业务正确性锁的职责。

## 事务与 SqlSugar

`CacheEvict` 的执行顺序为：提前捕获 Key → 获取与查询相同的 `:load-lock` → 执行 add/update 方法 → 成功后批量删除缓存 → 逆序释放锁。`Condition`、Key 和上下文在方法执行前捕获，批量结果提前物化并去重，避免 Save 回填 ID 或修改集合后删除错误 Key。条件为 false 或批量结果为空时仍执行业务方法，但不获取缓存锁或删除缓存。业务异常不触发失效；获取锁、业务或删除失败均释放已持有的锁。

批量更新按 Key 的固定顺序获取全部锁后才执行业务。等待所有锁共享策略的 `LockWaitTimeout`，按 `LockPollInterval` 重试，租约使用 `LockExpiry` 并自动续期；等待超时抛出 `TimeoutException`，调用方取消保持取消异常。业务执行不使用查询的 `QueryTimeout`。缓存命中仍直接返回，允许更新期间读到旧缓存；正常持锁加载的旧查询先完成回填，更新随后取得锁并删除，下一次未命中查询加载新数据。

特性不创建事务、不注册提交回调、不等待外层事务，也不判断业务是否已提交。事务定义和提交时机由开发者负责；若要求删除后刷新读取已提交数据，应让提交发生在被拦截的方法完成之前。若方法内部再次调用同 Key 的未命中查询，可能等待自身持有的加载锁并超时，应避免这种调用顺序。

失效通过 `ICache.RemoveManyAsync` 一次提交捕获的 Key 集合，返回实际删除条目数；已不存在的 Key 不算失败。Redis 单机使用一次批量删除，Cluster 按哈希槽分组并发删除，同槽多 Key 一次提交；不同组可能部分成功，不保证跨槽原子性。输入提前验证、重复 Key 去重，空集合不发送命令。接口默认实现并发调用 `RemoveAsync`，兼容既有自定义提供者；支持原生批量操作的提供者可覆盖该方法。

活动事务中的查询绕过共享缓存，避免缓存未提交数据。SqlSugar 注册器提供 `HasActiveTransaction`；其他适配器应实现该属性和真实提交回调，默认接口实现保守返回 `true`。未注册事务适配器时，按无事务处理；直接通过数据库连接启动、框架未跟踪的事务不受此机制管理。

缓存层直接通过公共抽象 `IDbTenantAccessor.GetTenantId()` 读取最终租户（包括 `UseTenant` 覆盖）；未注册访问器或返回 null 时，缓存租户维度为 null，不回退到身份租户。数据源固定，不加入 Key。跨租户、忽略租户或软删除过滤、启用数据权限及分表等特殊查询不应使用 `Cacheable`，由开发者保证使用边界；框架不检测这些状态或自动绕过缓存。后台任务需先 `using var lifecycle = serviceProvider.InitLifecycle()`，与现有 SqlSugar 用法一致。

这是最终一致性缓存。获取更新锁或删除失败会记录日志并向调用方传播，删除失败后缓存仍可能依赖 TTL 收敛。租约失效、更新完成后进程崩溃或开发者在方法返回后才提交外层事务等边界仍可能产生旧缓存，当前未使用版本校验或 outbox。

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

// 未指定表达式时只取首参数，其余参数不参与锁 Key
[TryLock("product", "正在处理")]
Task UpdateNameAsync(long productId, string name);
```

`Id > 0` 仅为业务条件示例，应按业务实际规则区分新增和更新。新增如果影响已有的空结果缓存，也需要失效对应 Key。
`TryLock` 优先使用显式 `Key` 或 `Keys`，两者不能同时指定。均未指定时固定取第一个方法参数：`string`、`long`、`int` 生成单个 Key；兼容 `ITryLockKey.GetKey()` 和 `ITryLockKeys.GetKeys()`。其余参数不参与 Key，不自动跳过首位的 `CancellationToken`，不序列化 DTO 或集合；无参数、null 或不支持的类型会报配置错误。Key 不在首位时使用显式表达式，不提供 `KeyParameterIndex`。

`ITryLockKey` 和 `ITryLockKeys` 保留原命名空间及签名，均标记 `[Obsolete(..., false)]`，仅产生作废警告，将在两个版本后移除。旧 DTO 暂时可继续使用，迁移时分别改为 `Key = "input.Id"` 和 `Keys = "inputs.Select(x => x.Id)"` 等业务表达式。

简单值以固定文化格式转为字符串，锁格式保持 `Locker:{scene}:{key}`；多锁按 Key 去重和排序后获取、逆序释放，空集合或全部无效的 Key 报错。
直接使用 `MultiLock.AcquireAsync` 时传入字符串 Key 集合。自定义锁提供者需注册 `AddDistributedLockOptions()`；使用显式表达式时另需将两个表达式组件注册为单例，默认首参数路径不依赖表达式组件。

缓存 Key 的统一规则改变了既有缓存格式，发布时可递增策略 `Version` 隔离不同版本；锁 Key 的字符串格式保持兼容。

`IDbTenantAccessor` 已移至公共抽象层，命名空间为 `Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context`。缓存层不依赖 SqlSugar 实现。
