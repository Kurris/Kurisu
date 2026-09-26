# Kurisu.Expressions

基于 Roslyn 的独立 C# 表达式库，目标框架为 .NET 8，不依赖 Kurisu 的缓存、AOP 或 ASP.NET Core 模块。

```csharp
using Kurisu.Expressions;

var compiler = new ExpressionCompiler();
var getKey = await compiler.CompileAsync<Func<ProductInput, long>>("input.Id", "input");
var shouldEvict = await compiler.CompileAsync<Func<ProductInput, bool>>("input.Id > 0", "input");
var getKeys = await compiler.CompileAsync<Func<List<ProductInput>, long[]>>(
    "inputs.Where(x => x.Id > 0).Select(x => x.Id).Distinct().ToArray()", "inputs");

long key = getKey(new ProductInput { Id = 42 });

public sealed class ProductInput
{
    public long Id { get; set; }
}
```

参数名支持 `@event` 等 C# 转义标识符，按委托签名顺序绑定，参数类型及返回类型由委托确定。无参数表达式使用 `await compiler.CompileAsync<Func<int>>("42")`。
默认导入 `System`、`System.Collections.Generic` 和 `System.Linq`；自动引用委托签名涉及的程序集。
其他工具类、成员类型所在程序集及命名空间可通过构造函数的 `references`、`imports` 添加。
业务类型和访问的成员须对生成的程序集可见。

编译不会执行表达式正文。语法和类型错误在 `CompileAsync` 时抛出 Roslyn 的 `CompilationErrorException`，其中包含诊断信息；业务异常在委托调用时直接传播。

同一个编译器实例按表达式文本、委托类型和参数名顺序缓存委托。重复调用 `CompileAsync` 返回同一委托，并发首次调用共享一次编译；编译失败后允许重试。
应复用编译器实例。缓存随实例持有，不设置过期时间；不同实例的程序集引用和命名空间配置相互隔离。
编译器实例可并发使用，委托调用的线程安全还取决于表达式访问的业务对象。

支持 C# 表达式，包括成员访问、运算、条件、空值处理及 LINQ/Lambda，不接受语句脚本。
运行时编译依赖 Roslyn，不能用于 Native AOT。表达式按应用代码执行，适用于开发者定义的表达式，不提供不受信任脚本的隔离环境。

仅提供异步编译接口，不同步阻塞 Roslyn 的异步执行。编译本身仍包含同步的 CPU 工作，不使用 Task.Run 包装。

编译取消令牌通过 `CompileAsync<TDelegate>(expression, parameterNames, cancellationToken)` 传入，参数名使用数组，例如 `["input"]`；无参数时传入 `[]`。

取消令牌只取消当前调用的等待，不取消共享编译，也不会移除成功编译的缓存。Roslyn 同步编译阶段不支持通过这个等待令牌中断。

## 方法参数绑定

`MethodExpressionEvaluator` 在编译器之上提供基于 `MethodInfo` 的参数绑定，用于 AOP 等只在运行时知道方法签名的场景：

```csharp
var evaluator = new MethodExpressionEvaluator(compiler);
var value = await evaluator.EvaluateAsync<object>(method, "input.Id", arguments, cancellationToken);
var condition = await evaluator.EvaluateAsync<bool>(method, "input.Id > 0", arguments, cancellationToken);
```

复用 evaluator 实例即可复用方法调用适配器；每次调用只传入本次参数，不捕获服务实例或业务参数。
