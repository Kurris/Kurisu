using Microsoft.CodeAnalysis.Scripting;
using Xunit;

namespace Kurisu.Test.Expressions;

public class ExpressionCompilerTests
{
    private readonly Kurisu.Expressions.ExpressionCompiler _compiler = new();

    [Fact]
    public async Task BindsParametersByNameAndReusesDelegate()
    {
        var evaluate = await _compiler.CompileAsync<Func<int, int, int>>("left * 10 + right", "left", "right");
        Assert.Equal(23, evaluate(2, 3));
        Assert.Equal(54, evaluate(5, 4));
    }

    [Fact]
    public async Task SupportsConstantAndNullResults()
    {
        Assert.Equal(42, (await _compiler.CompileAsync<Func<int>>("42"))());
        Assert.Null((await _compiler.CompileAsync<Func<string?>>("null"))());
    }

    [Fact]
    public async Task SupportsMemberAccessConditionAndBatchLinq()
    {
        var key = await _compiler.CompileAsync<Func<Input, long>>("input.Id", "input");
        var condition = await _compiler.CompileAsync<Func<Input, bool>>("input.Id > 0", "input");
        var keys = await _compiler.CompileAsync<Func<List<Input>, long[]>>(
            "inputs.Where(x => x.Id > 0).Select(x => x.Id).Distinct().ToArray()", "inputs");
        Assert.Equal(7, key(new Input { Id = 7 }));
        Assert.False(condition(new Input()));
        Assert.True(condition(new Input { Id = 7 }));
        Assert.Equal(new long[] { 7 }, keys([new(), new() { Id = 7 }, new() { Id = 7 }]));
    }

    [Fact]
    public async Task CompilationDoesNotExecuteBusinessCodeAndInvocationPreservesExceptions()
    {
        var evaluate = await _compiler.CompileAsync<Func<Input, long>>("input.Read()", "input");
        var input = new Input();
        Assert.Equal(0, input.Calls);
        Assert.Throws<InvalidOperationException>(() => evaluate(input));
        Assert.Equal(1, input.Calls);
    }

    [Fact]
    public async Task CompiledDelegateDoesNotShareInvocationArguments()
    {
        var evaluate = await _compiler.CompileAsync<Func<Input, long>>("input.Id", "input");
        var results = await Task.WhenAll(Enumerable.Range(0, 50)
            .Select(i => Task.Run(() => evaluate(new Input { Id = i }))));
        Assert.Equal(Enumerable.Range(0, 50).Select(i => (long)i), results);
    }

    [Theory]
    [InlineData("input.Unknown")]
    [InlineData("input.Id +")]
    [InlineData("\"wrong return type\"")]
    [InlineData("input.Id; return 1;")]
    public async Task ReportsCompilationErrors(string expression)
    {
        var error = await Assert.ThrowsAsync<CompilationErrorException>(() =>
            _compiler.CompileAsync<Func<Input, long>>(expression, "input"));
        Assert.NotEmpty(error.Diagnostics);
    }

    [Fact]
    public async Task ValidatesParameterBindings()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _compiler.CompileAsync<Func<int, int>>("x"));
        await Assert.ThrowsAsync<ArgumentException>(() => _compiler.CompileAsync<Func<int, int>>("x", "x) => 1;"));
        await Assert.ThrowsAsync<ArgumentException>(() => _compiler.CompileAsync<Func<int, int, int>>("x", "x", "x"));
    }

    [Fact]
    public async Task SupportsAdditionalReferencesAndImports()
    {
        var compiler = new Kurisu.Expressions.ExpressionCompiler(
            [typeof(System.Text.Json.JsonSerializer).Assembly], ["System.Text.Json"]);
        var evaluate = await compiler.CompileAsync<Func<int, string>>("JsonSerializer.Serialize(value, (JsonSerializerOptions)null)", "value");
        Assert.Equal("42", evaluate(42));
    }

    [Fact]
    public async Task SupportsCompileCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _compiler.CompileAsync<Func<int>>("42", [], cancellation.Token));
    }

    [Fact]
    public async Task ReusesDelegateForEquivalentParameterArrays()
    {
        var names = new[] { "input" };
        var first = await _compiler.CompileAsync<Func<Input, long>>("input.Id", names);
        names[0] = "changed";
        var second = await _compiler.CompileAsync<Func<Input, long>>("input.Id", ["input"]);
        Assert.Same(first, second);
        Assert.Equal(9, second(new Input { Id = 9 }));
    }

    [Fact]
    public async Task ConcurrentCompilationsReturnSameDelegate()
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requests = Enumerable.Range(0, 20).Select(_ => Task.Run(async () =>
        {
            await start.Task;
            return await _compiler.CompileAsync<Func<int, int>>("value + 1", "value");
        })).ToArray();
        start.SetResult();
        var delegates = await Task.WhenAll(requests);
        Assert.All(delegates, value => Assert.Same(delegates[0], value));
    }

    [Fact]
    public async Task CacheSeparatesExpressionDelegateTypeAndParameterOrder()
    {
        var first = await _compiler.CompileAsync<Func<int, int, int>>("left - right", "left", "right");
        var reordered = await _compiler.CompileAsync<Func<int, int, int>>("left - right", "right", "left");
        var differentExpression = await _compiler.CompileAsync<Func<int, int, int>>("left + right", "left", "right");
        var differentType = await _compiler.CompileAsync<Func<long, long, long>>("left - right", "left", "right");
        Assert.Equal(3, first(5, 2));
        Assert.Equal(-3, reordered(5, 2));
        Assert.Equal(7, differentExpression(5, 2));
        Assert.Equal(3L, differentType(5, 2));
    }

    [Fact]
    public async Task CompilersDoNotShareCachedDelegates()
    {
        var first = await _compiler.CompileAsync<Func<int>>("42");
        var other = new Kurisu.Expressions.ExpressionCompiler();
        Assert.NotSame(first, await other.CompileAsync<Func<int>>("42"));
    }

    [Fact]
    public async Task FailedCompilationsCanRetry()
    {
        var first = await Assert.ThrowsAsync<CompilationErrorException>(() =>
            _compiler.CompileAsync<Func<int>>("unknown"));
        var retry = await Assert.ThrowsAsync<CompilationErrorException>(() =>
            _compiler.CompileAsync<Func<int>>("unknown"));
        Assert.NotSame(first, retry);
    }

    [Fact]
    public async Task CancellationDoesNotEvictCachedDelegate()
    {
        var first = await _compiler.CompileAsync<Func<int>>("42");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _compiler.CompileAsync<Func<int>>("42", [], cancellation.Token));
        Assert.Same(first, await _compiler.CompileAsync<Func<int>>("42"));
    }

    [Fact]
    public async Task EscapedIdentifiers_AreSupportedAndAliasesCannotDuplicateParameters()
    {
        var evaluate = await _compiler.CompileAsync<Func<int, int>>("@event + 1", "@event");
        Assert.Equal(8, evaluate(7));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _compiler.CompileAsync<Func<int, int, int>>("value", "value", "@value"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" value")]
    [InlineData("value ")]
    [InlineData("value,other")]
    [InlineData("value\0other")]
    [InlineData("value/*comment*/")]
    [InlineData(null)]
    public async Task InvalidParameterNames_CannotHitCachedDelegate(string? name)
    {
        await _compiler.CompileAsync<Func<int, int>>("value", "value");
        await Assert.ThrowsAsync<ArgumentException>(() => _compiler.CompileAsync<Func<int, int>>("value", [name!]));
    }

    [Fact]
    public async Task NullSafeNavigationAndNestedLinq_UseCurrentValues()
    {
        var getId = await _compiler.CompileAsync<Func<Input?, long>>("input?.Id ?? -1", "input");
        Assert.Equal(-1, getId(null));
        Assert.Equal(7, getId(new Input { Id = 7 }));
        var select = await _compiler.CompileAsync<Func<List<List<int>>, int[]>>(
            "groups.SelectMany(items => items.Where(value => value > 0)).Distinct().ToArray()", "groups");
        Assert.Equal(new[] { 1, 2 }, select([[0, 1], [1, 2]]));
    }

    public sealed class Input
    {
        public long Id { get; set; }
        public int Calls { get; private set; }
        public long Read()
        {
            Calls++;
            throw new InvalidOperationException("business failure");
        }
    }
}
