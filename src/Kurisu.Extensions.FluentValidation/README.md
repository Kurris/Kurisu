# Kurisu.Extensions.FluentValidation

可选的 FluentValidation 参数验证扩展，适用于 .NET 8。模型实现 `IAopValidatableObject` 并在模型内定义规则，独立方法拦截器自动执行同步和异步验证，无需逐个给服务参数加属性。验证失败时抛出 `ParameterValidationException`，由宿主决定 HTTP 响应格式。验证入口统一为模型接口, 不提供参数验证属性或程序集扫描注册。

## 启用动态代理与自动验证

接口位于 `Kurisu.Extensions.FluentValidation.Abstractions`，验证错误与异常位于 `Kurisu.Extensions.FluentValidation.Result`。`ModelValidation` 和服务注册扩展保留在根命名空间。项目内部按 `Abstractions / Aop / Internal / Result` 分目录组织。

引用本项目后，使用动态代理启动宿主：

```csharp
using AspectCore.Extensions.Hosting;
using Kurisu.AspNetCore.Startup;

KurisuHost.Builder(args).UseDynamicProxy().RunKurisu<Startup>();
```

在继承 `DefaultStartup` 的启动类中，先执行框架注册，再启用模型接口识别：

```csharp
using Kurisu.Extensions.FluentValidation;
using Kurisu.Extensions.FluentValidation.Abstractions;
using Kurisu.Extensions.FluentValidation.Result;

public override void ConfigureServices(IServiceCollection services)
{
    base.ConfigureServices(services);
    services.AddParameterValidation();
}
```

`AddParameterValidation()` 启用模型接口自动识别, 不接收程序集扫描参数。扩展通过官方 AspectCore 注册 `ParameterValidationInterceptor` 方法拦截器, 无需调用 `EnableParameterAspect()`。代理容器仍使用微软 DI。注册时按方法参数的声明类型筛选, 普通服务不会仅因启用验证而被代理。

默认在第一个无效模型参数处停止验证。需要一次收集所有模型参数的字段错误时，显式启用：

```csharp
services.AddParameterValidation(collectAllErrors: true);
```

收集模式按参数顺序执行规则，存在任何错误时都不进入业务方法或默认事务。配置错误、规则内部异常和取消异常立即终止，不合并为字段错误。重复注册只保留一个验证拦截器配置；无参数注册保留已有策略，显式策略以最后一次为准。

## 在模型中定义规则

实现接口的模型在 `CreateValidator(IServiceProvider serviceProvider)` 中返回适用于自己的 `IValidator`，规则使用标准 FluentValidation API：

```csharp
using FluentValidation;
using Kurisu.Extensions.FluentValidation;
using Kurisu.Extensions.FluentValidation.Abstractions;
using Kurisu.Extensions.FluentValidation.Result;

public sealed record CreateItemRequest(string SkuId, int Quantity) : IAopValidatableObject
{
    public IValidator CreateValidator(IServiceProvider services) =>
        ModelValidation.Create<CreateItemRequest>(validator =>
    {
        validator.RuleFor(request => request.SkuId).NotEmpty();
        validator.RuleFor(request => request.Quantity).GreaterThan(0);
    });
}
```

服务方法直接接收模型，不需要验证属性。以下最小示例仅演示调用入口：

```csharp
using Kurisu.AspNetCore.Abstractions.DependencyInjection;

public interface IItemApplication
{
    Task<string> CreateAsync(CreateItemRequest request, CancellationToken token);
}

[DiInject]
public class ItemApplication : IItemApplication
{
    public virtual Task<string> CreateAsync(CreateItemRequest request, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult(request.SkuId);
    }
}
```

调用方应注入 `IItemApplication`，通过代理调用 `CreateAsync`。自行 `new ItemApplication()` 或在实现类内部直接调用自己的方法，不会经过这个代理入口。使用类代理时，方法需要可重写，因此用例将应用方法声明为 `virtual`。

模型参数的声明类型实现 `IAopValidatableObject` 时，方法拦截器自动执行验证。泛型参数在调用时根据闭合类型与模型实例判断。普通类型不会仅因为注册了外部验证器就自动执行验证。

接口代理与类代理均按参数位置执行一次验证。只有参数声明类型实现 `IAopValidatableObject` 时才自动验证; 声明为 `object` 或未继承验证接口的基础类型时不自动验证。泛型参数保守保留代理入口, 调用时按闭合参数类型判断是否验证。

`CreateValidator` 接收当前调用作用域的 `IServiceProvider`，规则可以解析该作用域中的依赖。接口模型为 `null` 时，仍按声明类型返回“参数不能为空”的字段错误；模型返回空验证器时抛出 `InvalidOperationException`，用于暴露配置错误。

根模型与嵌套模型共用验证器检查：返回的验证器必须支持模型的实际类型，基础类型的验证器可以用于其派生模型。空验证器或类型不匹配属于配置错误，抛出 `InvalidOperationException`；规则不通过才属于参数验证失败。

扩展注册的 `ParameterValidationInterceptor` 在方法执行前验证参数，`Order` 为 `-9`，早于默认 `[Transactional]` 的 `Order = 0`。本扩展无需设置自定义顺序；需要数据库事务时，在方法上保留 `[Transactional]`，模型验证通过后才进入事务并执行业务。

异步规则使用 `MustAsync / CustomAsync / WhenAsync` 等 API，扩展始终调用 `ValidateAsync`，也会执行同步规则。方法传入的 `CancellationToken` 会交给验证器，验证器自身抛出的异常和取消异常继续向外传播。ASP.NET MVC 的同步自动验证管道无法执行异步规则；本扩展通过官方 AspectCore 的方法拦截器异步执行。参见 [FluentValidation 的 ASP.NET Core 接入说明](https://docs.fluentvalidation.net/en/latest/aspnet.html)与[异步验证说明](https://docs.fluentvalidation.net/en/latest/async.html)。

## 组合嵌套模型

父模型通过 `ModelValidation.For<T>(services)` 调用子模型的规则，保留父级属性路径，例如 `Request.Quantity`：

```csharp
public sealed record CreateItemCommand(CreateItemRequest Request) : IAopValidatableObject
{
    public IValidator CreateValidator(IServiceProvider services) =>
        ModelValidation.Create<CreateItemCommand>(validator =>
    {
        validator.RuleFor(command => command.Request).NotNull()
            .SetValidator(ModelValidation.For<CreateItemRequest>(services));
    });
}
```

必填子模型在父规则中使用 `NotNull()`；`SetValidator` 会跳过空的子模型。嵌套规则同样异步执行，并使用当前调用作用域和取消令牌。

`For<T>` 直接传递 FluentValidation 的子验证上下文，保留集合索引、`RootContextData`、规则选择器以及错误的 `ErrorCode / Severity / CustomState`。子规则全为同步规则时，也支持同步 `Validate`。`ParameterValidationException` 对外仍只提供参数名、字段路径和错误消息。

父模型也必须实现 `IAopValidatableObject`, 并主动配置子模型规则。仅子模型实现接口时, 传入父模型不会自动递归扫描或验证所有属性。

## 复用独立验证器

模型可以在 `CreateValidator` 中复用通过标准 DI 注册的独立验证器:

```csharp
public sealed record CreateItemRequest(string SkuId) : IAopValidatableObject
{
    public IValidator CreateValidator(IServiceProvider services)
        => services.GetRequiredService<IValidator<CreateItemRequest>>();
}

public sealed class CreateItemRequestValidator : AbstractValidator<CreateItemRequest>
{
    public CreateItemRequestValidator()
        => RuleFor(request => request.SkuId).NotEmpty();
}

services.AddScoped<IValidator<CreateItemRequest>, CreateItemRequestValidator>();
services.AddParameterValidation();
```

无法修改的第三方模型通过实现 `IAopValidatableObject` 的请求模型包装, 在该模型的验证器中验证内部对象。注册 `IValidator<T>` 本身不会让普通参数自动触发验证。

拦截器按接口方法与实现方法缓存验证参数的位置和名称，闭合泛型类型分别缓存。每次调用只读取一次取消令牌，没有可验证参数时直接执行后续方法。缓存不保存模型、验证器或作用域服务。需要减少规则重复构建时，可采用上面的 `AddScoped<IValidator<T>, ...>()`；内联 `ModelValidation.Create<T>` 仍会在每次调用时构建规则。捕获作用域依赖的父验证器和 `For<T>` 适配器应随作用域使用。

同一作用域中的多次验证会复用 scoped 验证器和规则；新作用域获得独立实例。验证器应读取本次传入的模型，不要在规则中捕获某一次请求对象。扩展不会自动从 DI 查找验证器，是否使用 DI 仍由模型的 `CreateValidator` 决定。

## 在后台任务或消息消费中显式验证

没有代理入口时可以直接调用，不需要注册 `AddParameterValidation()`：

```csharp
await ModelValidation.ValidateAsync(request, services, cancellationToken);
```

`request` 必须实现 `IAopValidatableObject`，`services` 是当前调用作用域的服务提供者。验证通过后正常完成，失败时抛出与 AOP 相同的 `ParameterValidationException`。显式入口的错误参数名为 `model`，字段路径和规则消息保持原样，支持嵌套规则、异步规则和取消令牌。

规则消息可以使用业务标识，例如 `.WithMessage("biz_user_login_fail")`，扩展不会翻译或改写消息，也不增加单独的错误码字段。

## 字段错误与异常处理

`ParameterValidationException` 继承框架 `UserFriendlyException`，消息为“参数验证失败”。`Errors` 是 `IReadOnlyList<ParameterValidationError>`，每项包含：

| 属性        | 含义                                                      |
|-------------|-----------------------------------------------------------|
| `Parameter` | 应用方法的参数名，如 `request`                            |
| `Field`     | 验证器返回的属性路径，如 `Quantity` 或 `Request.Quantity` |
| `Message`   | 该字段的验证错误消息                                      |

扩展不直接生成 HTTP 响应。需要返回字段列表时，在宿主中继承 `DefaultExceptionHandlers` 并添加对应处理器：

```csharp
using Kurisu.AspNetCore.Abstractions.Result;
using Kurisu.AspNetCore.UnifyResultAndValidation;
using Kurisu.AspNetCore.UnifyResultAndValidation.Attributes;
using Kurisu.Extensions.FluentValidation;
using Kurisu.Extensions.FluentValidation.Abstractions;
using Kurisu.Extensions.FluentValidation.Result;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

public sealed class AppExceptionHandlers(
    IApiResult apiResult,
    IHttpContextAccessor httpContextAccessor,
    ILogger<DefaultExceptionHandlers> logger)
    : DefaultExceptionHandlers(apiResult, httpContextAccessor, logger)
{
    [HandleException<ParameterValidationException>]
    public void ParameterValidationExceptionHandle(ParameterValidationException exception)
        => exception.ExceptionContext.Result = new ObjectResult(
            new ApiResult<IReadOnlyList<ParameterValidationError>>
            {
                Code = ApiStateCode.ValidateError,
                Msg = exception.Message,
                Data = exception.Errors
            });
}
```

在 `base.ConfigureServices(services)` 之后替换框架异常处理服务，保留默认的其他异常处理：

```csharp
using Kurisu.AspNetCore.UnifyResultAndValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

services.Replace(ServiceDescriptor.Singleton<IFrameworkExceptionHandlers, AppExceptionHandlers>());
```

该处理器沿用默认统一包装规则，返回的业务码为 `400`，客户端应检查响应的 `code`。框架异常处理器按异常的具体类型精确匹配；宿主必须注册 `[HandleException<ParameterValidationException>]`，才能将验证失败按上述格式返回。若没有专门处理器，该异常会进入默认 `Exception` 处理并返回 HTTP 500，不会因为继承 `UserFriendlyException` 就自动使用基类处理器。

回归测试见 [Kurisu.Test.FluentValidation](../../test/Kurisu.Test.FluentValidation)：验证在事务前执行, 普通服务不被验证功能代理, 模型可复用作用域内注册的验证器, 宿主负责统一字段错误响应。
