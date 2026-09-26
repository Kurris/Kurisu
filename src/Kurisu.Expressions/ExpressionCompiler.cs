using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace Kurisu.Expressions;

/// <summary>
/// 将 C# 表达式编译为强类型委托。实例可并发使用，并缓存已编译的委托。
/// </summary>
public sealed class ExpressionCompiler
{
    private readonly ScriptOptions _options;
    private readonly ConcurrentDictionary<CompilationKey, Lazy<Task<Delegate>>> _compiled = new();

    private sealed record CompilationKey(Type DelegateType, string Expression, string ParameterNames);

    /// <summary>
    /// 创建编译器，默认导入 System、System.Collections.Generic 和 System.Linq。
    /// 委托签名中的类型会自动添加程序集引用。
    /// </summary>
    /// <param name="references">表达式额外使用的业务类型或工具类所在程序集。</param>
    /// <param name="imports">表达式额外使用的命名空间。</param>
    public ExpressionCompiler(IEnumerable<Assembly>? references = null, IEnumerable<string>? imports = null)
    {
        _options = ScriptOptions.Default
            .AddReferences(typeof(Enumerable).Assembly)
            .AddReferences(references ?? [])
            .AddImports("System", "System.Collections.Generic", "System.Linq")
            .AddImports(imports ?? [])
            .WithOptimizationLevel(OptimizationLevel.Release);
    }

    /// <summary>
    /// 编译一个具有返回值的 C# 表达式；编译过程不会执行表达式。
    /// 参数名按委托参数顺序绑定，参数和返回值类型由委托签名决定。
    /// </summary>
    /// <typeparam name="TDelegate">具有返回值的委托类型，例如 Func&lt;Input, long&gt;。</typeparam>
    /// <param name="expression">表达式正文，例如 input.Id 或 inputs.Select(x =&gt; x.Id).ToArray()。</param>
    /// <param name="parameterNames">表达式使用的参数名，无参数表达式可省略。</param>
    /// <returns>可重复调用的委托；业务异常在调用时直接传播。</returns>
    /// <exception cref="ArgumentException">表达式为空或委托参数配置无效。</exception>
    /// <exception cref="CompilationErrorException">表达式存在语法、类型或成员访问错误。</exception>
    public Task<TDelegate> CompileAsync<TDelegate>(string expression, params string[] parameterNames)
        where TDelegate : Delegate
        => CompileAsync<TDelegate>(expression, parameterNames, CancellationToken.None);

    /// <summary>
    /// 异步取得缓存的表达式委托，同一实例中的相同表达式并发共享一次编译。
    /// </summary>
    /// <typeparam name="TDelegate">目标委托类型。</typeparam>
    /// <param name="expression">C# 表达式正文。</param>
    /// <param name="parameterNames">按委托签名顺序绑定的参数名。</param>
    /// <param name="cancellationToken">取消本次等待，不取消其他调用方共享的编译。</param>
    /// <returns>可重复调用的委托。</returns>
    public async Task<TDelegate> CompileAsync<TDelegate>(string expression, string[] parameterNames, CancellationToken cancellationToken) where TDelegate : Delegate
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        ArgumentNullException.ThrowIfNull(parameterNames);

        var names = parameterNames.ToArray();
        ValidateParameterNames(names);

        // 参数名已经验证为标识符，使用空字符分隔不会产生歧义。
        var key = new CompilationKey(typeof(TDelegate), expression, string.Join("\0", names));
        var entry = _compiled.GetOrAdd(key, _ => new Lazy<Task<Delegate>>(
            () => CompileCoreAsync<TDelegate>(expression, names)));
        var compilation = entry.Value;
        try
        {
            return (TDelegate)await compilation.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch when (compilation.IsFaulted)
        {
            // 只移除本次失败的条目，避免并发调用误删后续重试。
            ((ICollection<KeyValuePair<CompilationKey, Lazy<Task<Delegate>>>>)_compiled)
                .Remove(new(key, entry));
            throw;
        }
    }

    private async Task<Delegate> CompileCoreAsync<TDelegate>(string expression, string[] parameterNames)
        where TDelegate : Delegate
    {
        var signature = GetSignature<TDelegate>(parameterNames.Length);
        // 先按单个表达式解析，避免将语句或脚本误当作表达式正文。
        var syntax = SyntaxFactory.ParseExpression(expression, consumeFullText: true);
        var errors = syntax.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToImmutableArray();
        if (errors.Length != 0)
            throw new CompilationErrorException("表达式语法错误。", errors);

        // 脚本只创建 Lambda；业务表达式直到调用返回的委托时才执行。
        var source = $"({string.Join(", ", parameterNames)}) => (\n{expression}\n)";
        var script = CSharpScript.Create<TDelegate>(source, _options.AddReferences(GetReferences(typeof(TDelegate), signature)));
        var runner = script.CreateDelegate();
        return await runner().ConfigureAwait(false);
    }

    private static void ValidateParameterNames(string[] names)
    {
        var uniqueNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in names)
        {
            var token = SyntaxFactory.ParseToken(name ?? "");
            if (!token.IsKind(SyntaxKind.IdentifierToken) || token.ContainsDiagnostics ||
                token.Span.Length != name?.Length || !uniqueNames.Add(token.ValueText))
                throw new ArgumentException("参数名必须是唯一的 C# 标识符。", "parameterNames");
        }
    }

    private static MethodInfo GetSignature<TDelegate>(int parameterCount) where TDelegate : Delegate
    {
        var signature = typeof(TDelegate).GetMethod("Invoke")
            ?? throw new ArgumentException("必须指定具体的委托类型。", nameof(TDelegate));
        var parameters = signature.GetParameters();
        if (signature.ReturnType == typeof(void) || signature.ReturnType.IsByRef ||
            parameters.Any(parameter => parameter.ParameterType.IsByRef))
            throw new ArgumentException("表达式委托必须具有返回值，且不支持 ref、in 或 out 参数及 ref 返回值。", nameof(TDelegate));
        if (parameters.Length != parameterCount)
            throw new ArgumentException("参数名数量必须与委托参数一致。", "parameterNames");
        return signature;
    }

    private static HashSet<Assembly> GetReferences(Type delegateType, MethodInfo signature)
    {
        var assemblies = new HashSet<Assembly>();
        AddTypeReferences(delegateType, assemblies);
        AddTypeReferences(signature.ReturnType, assemblies);
        foreach (var parameter in signature.GetParameters())
            AddTypeReferences(parameter.ParameterType, assemblies);
        return assemblies;
    }

    private static void AddTypeReferences(Type type, HashSet<Assembly> assemblies)
    {
        assemblies.Add(type.Assembly);
        if (type.HasElementType)
            AddTypeReferences(type.GetElementType()!, assemblies);
        foreach (var argument in type.GetGenericArguments())
            AddTypeReferences(argument, assemblies);
    }
}
