using System.Reflection;
using Kurisu.Expressions;
using Microsoft.CodeAnalysis.Scripting;
using Xunit;

namespace Kurisu.Test.Expressions;

public class MethodExpressionEvaluatorTests
{
    private readonly MethodExpressionEvaluator _evaluator = new(new ExpressionCompiler());
    private static MethodInfo Method(string name) => typeof(Signatures).GetMethod(name)!;

    [Fact]
    public async Task SameExpression_BindsDifferentMethodParameterOrders()
    {
        Assert.Equal(7, await _evaluator.EvaluateAsync<int>(Method(nameof(Signatures.Forward)), "left - right", [10, 3]));
        Assert.Equal(-7, await _evaluator.EvaluateAsync<int>(Method(nameof(Signatures.Reverse)), "left - right", [10, 3]));
        Assert.Equal(7L, await _evaluator.EvaluateAsync<long>(Method(nameof(Signatures.Long)), "left - right", [10L, 3L]));
    }

    [Fact]
    public async Task ResultTypes_DoNotShareIncompatibleAdapters()
    {
        var method = Method(nameof(Signatures.Forward));
        Assert.True(await _evaluator.EvaluateAsync<bool>(method, "left > right", [2, 1]));
        Assert.Equal(true, await _evaluator.EvaluateAsync<object>(method, "left > right", [2, 1]));
        await Assert.ThrowsAsync<CompilationErrorException>(() => _evaluator.EvaluateAsync<int>(method, "left > right", [2, 1]));
        Assert.False(await _evaluator.EvaluateAsync<bool>(method, "left > right", [1, 2]));
    }

    [Fact]
    public async Task KeywordParameter_UsesEscapedCSharpIdentifier()
    {
        Assert.Equal(8, await _evaluator.EvaluateAsync<int>(Method(nameof(Signatures.Keyword)), "@event + 1", [7]));
    }

    [Fact]
    public async Task NoParametersAndNullableArguments_AreSupported()
    {
        Assert.Equal(42, await _evaluator.EvaluateAsync<int>(Method(nameof(Signatures.None)), "42", []));
        var method = Method(nameof(Signatures.Nullable));
        Assert.Equal(-1, await _evaluator.EvaluateAsync<int>(method, "value ?? -1", [null]));
        Assert.Equal(7, await _evaluator.EvaluateAsync<int>(method, "value ?? -1", [7]));
    }

    [Fact]
    public async Task ConcurrentEvaluation_UsesEachCallsArguments()
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = Enumerable.Range(0, 20).Select(i => Task.Run(async () =>
        {
            await start.Task;
            return await _evaluator.EvaluateAsync<int>(Method(nameof(Signatures.Forward)), "left - right", [i, 1]);
        })).ToArray();
        start.SetResult();
        Assert.Equal(Enumerable.Range(-1, 20), await Task.WhenAll(calls));
    }

    [Fact]
    public async Task CompilationFailure_CanRetryWithoutPoisoningValidExpressions()
    {
        var method = Method(nameof(Signatures.None));
        var first = await Assert.ThrowsAsync<CompilationErrorException>(() => _evaluator.EvaluateAsync<int>(method, "missing", []));
        var retry = await Assert.ThrowsAsync<CompilationErrorException>(() => _evaluator.EvaluateAsync<int>(method, "missing", []));
        Assert.NotSame(first, retry);
        Assert.Equal(42, await _evaluator.EvaluateAsync<int>(method, "42", []));
    }

    [Fact]
    public async Task InvocationFailure_IsNotWrappedAndDoesNotPoisonAdapter()
    {
        var method = Method(nameof(Signatures.Forward));
        await Assert.ThrowsAsync<DivideByZeroException>(() => _evaluator.EvaluateAsync<int>(method, "left / right", [10, 0]));
        Assert.Equal(5, await _evaluator.EvaluateAsync<int>(method, "left / right", [10, 2]));
    }

    public static class Signatures
    {
        public static void Forward(int left, int right) { }
        public static void Reverse(int right, int left) { }
        public static void Long(long left, long right) { }
        public static void Keyword(int @event) { }
        public static void None() { }
        public static void Nullable(int? value) { }
    }
}
