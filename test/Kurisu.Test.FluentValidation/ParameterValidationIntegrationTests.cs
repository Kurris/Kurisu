using Kurisu.Extensions.FluentValidation.Abstractions;
using Kurisu.Extensions.FluentValidation.Result;
using System.Reflection;
using AspectCore.Extensions.DependencyInjection;
using FluentValidation;
using FluentValidation.Results;
using Kurisu.AspNetCore.Abstractions.DataAccess.Aop;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.Extensions.FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kurisu.Test.FluentValidation;

/// <summary>
/// 通过真实代理验证参数验证扩展及其与事务切面的执行顺序.
/// </summary>
public class ParameterValidationIntegrationTests
{
    private static ServiceProvider BuildProvider(bool? collectAllErrors = null)
    {
        var services = new ServiceCollection();
        services.AddScoped<CallState>();
        services.AddScoped<IDbContext>(provider => StubProxy.Create<IDbContext>((method, args) =>
        {
            var state = provider.GetRequiredService<CallState>();
            return method.Name switch
            {
                "get_DatasourceManager" => StubProxy.Create<IDatasourceManager>((member, _) =>
                    member.Name == "CreateTransScope" ? new TransactionScope(state) : throw new NotSupportedException()),
                _ => throw new NotSupportedException(method.Name)
            };
        }));
        services.AddScoped<IApplication, Application>();
        services.AddScoped<Application>();
        services.AddScoped<IPlainService, PlainService>();
        services.AddScoped<PlainService>();
        services.AddScoped<IValidator<ReusableModel>, ReusableModelValidator>();
        // 重复注册不应导致每个参数验证两次.
        services.AddParameterValidation();
        if (collectAllErrors.HasValue) services.AddParameterValidation(collectAllErrors.Value);
        services.AddParameterValidation();
        return services.BuildDynamicProxyProvider();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Validation_RunsOnceBeforeTransaction_InCurrentScope(bool classProxy)
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var state = scope.ServiceProvider.GetRequiredService<CallState>();
        var model = new Model(true);
        if (classProxy)
            await scope.ServiceProvider.GetRequiredService<Application>().SaveAsync(model);
        else
            await scope.ServiceProvider.GetRequiredService<IApplication>().SaveAsync(model);
        Assert.Equal(1, model.ValidationCount);
        Assert.Equal(new[] { "validate", "begin", "body", "commit", "transaction-dispose" }, state.Events);
    }

    [Fact]
    public async Task ValidationFailure_DoesNotStartTransactionOrBusinessMethod()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IApplication>();
        var error = await Assert.ThrowsAsync<ParameterValidationException>(() => service.SaveAsync(new Model(false)));
        Assert.Equal("model", Assert.Single(error.Errors).Parameter);
        Assert.Equal(new[] { "validate" }, scope.ServiceProvider.GetRequiredService<CallState>().Events);
    }

    [Fact]
    public async Task NullModel_IsRejectedBeforeTransaction()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        await Assert.ThrowsAsync<ParameterValidationException>(() =>
            scope.ServiceProvider.GetRequiredService<IApplication>().SaveAsync(null!));
        Assert.Empty(scope.ServiceProvider.GetRequiredService<CallState>().Events);
    }

    [Fact]
    public async Task OrdinaryServices_RemainUnproxied()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IPlainService>();
        Assert.IsType<PlainService>(service);
        Assert.IsType<PlainService>(scope.ServiceProvider.GetRequiredService<PlainService>());
        Assert.Equal("value", await service.EchoAsync("value"));
    }

    [Fact]
    public async Task ObjectParameter_DoesNotTriggerModelValidation()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IApplication>();
        var model = new Model(false);
        await service.OtherAsync(model);
        Assert.Equal(0, model.ValidationCount);
        Assert.Equal(new[] { "body" }, scope.ServiceProvider.GetRequiredService<CallState>().Events);
    }

    [Fact]
    public async Task Model_CanReuseRegisteredValidator()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IApplication>();
        await Assert.ThrowsAsync<ParameterValidationException>(() => service.ReuseAsync(new ReusableModel("")));
        Assert.Empty(scope.ServiceProvider.GetRequiredService<CallState>().Events);
        await service.ReuseAsync(new ReusableModel("valid"));
        Assert.Equal(new[] { "body" }, scope.ServiceProvider.GetRequiredService<CallState>().Events);
        var validator = scope.ServiceProvider.GetRequiredService<IValidator<ReusableModel>>();
        Assert.Equal(2, Assert.IsType<ReusableModelValidator>(validator).ValidationCount);
        Assert.Same(validator, new ReusableModel("valid").CreateValidator(scope.ServiceProvider));

        using var otherScope = provider.CreateScope();
        Assert.NotSame(validator, new ReusableModel("valid").CreateValidator(otherScope.ServiceProvider));
    }

    [Theory]
    [InlineData(null, "Child")]
    [InlineData("", "Child.Name")]
    public async Task NestedValidation_ReportsParentPropertyPath(string? name, string field)
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var parent = new ParentModel(name is null ? null! : new ReusableModel(name));
        var exception = await Assert.ThrowsAsync<ParameterValidationException>(() =>
            scope.ServiceProvider.GetRequiredService<IApplication>().NestedAsync(parent));
        Assert.Equal(field, Assert.Single(exception.Errors).Field);
        Assert.Empty(scope.ServiceProvider.GetRequiredService<CallState>().Events);
    }

    [Fact]
    public async Task NestedValidation_ExecutesBusinessMethodWhenChildIsValid()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IApplication>()
            .NestedAsync(new ParentModel(new ReusableModel("valid")));
        Assert.Equal(new[] { "body" }, scope.ServiceProvider.GetRequiredService<CallState>().Events);
    }

    [Fact]
    public async Task GenericMethod_ValidatesClosedModelOnce()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var model = new Model(true);
        await scope.ServiceProvider.GetRequiredService<IApplication>().GenericAsync(model);
        Assert.Equal(1, model.ValidationCount);
        Assert.Equal(new[] { "validate", "body" }, scope.ServiceProvider.GetRequiredService<CallState>().Events);
    }

    [Fact]
    public async Task GenericMethod_CachedPlansSeparateClosedTypesAndScopes()
    {
        using var provider = BuildProvider();
        for (var i = 0; i < 2; i++)
        {
            using var scope = provider.CreateScope();
            var application = scope.ServiceProvider.GetRequiredService<IApplication>();
            await application.UnconstrainedAsync("plain");
            var model = new Model(true);
            await application.UnconstrainedAsync(model);
            await application.UnconstrainedAsync("plain-again");
            Assert.Equal(1, model.ValidationCount);
            Assert.Equal(new[] { "body", "validate", "body", "body" },
                scope.ServiceProvider.GetRequiredService<CallState>().Events);
        }
    }

    [Fact]
    public async Task MultipleModels_AreEachValidatedBeforeBody()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var first = new Model(true);
        var second = new Model(true);
        await scope.ServiceProvider.GetRequiredService<IApplication>()
            .MultipleAsync("plain", first, second, CancellationToken.None, CancellationToken.None);
        Assert.Equal(1, first.ValidationCount);
        Assert.Equal(1, second.ValidationCount);
        Assert.Equal(new[] { "validate", "validate", "body" },
            scope.ServiceProvider.GetRequiredService<CallState>().Events);
    }

    [Fact]
    public async Task MultipleModels_FirstCancellationTokenStopsValidationAndBody()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var first = new Model(true);
        var second = new Model(true);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            scope.ServiceProvider.GetRequiredService<IApplication>()
                .MultipleAsync("plain", first, second, cancellation.Token, CancellationToken.None));
        Assert.Equal(0, first.ValidationCount);
        Assert.Equal(0, second.ValidationCount);
        Assert.Empty(scope.ServiceProvider.GetRequiredService<CallState>().Events);
    }

    [Theory]
    [InlineData(true, "未提供有效的验证器")]
    [InlineData(false, "不支持模型类型")]
    public async Task InvalidValidator_IsReportedBeforeTransaction(bool returnsNull, string message)
    {
        await using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scope.ServiceProvider.GetRequiredService<IApplication>().InvalidAsync(new InvalidModel(returnsNull)));

        Assert.Contains("request", exception.Message);
        Assert.Contains(message, exception.Message);
        Assert.Empty(scope.ServiceProvider.GetRequiredService<CallState>().Events);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    public async Task MultipleFailures_RespectConfiguredStrategy(bool collectAllErrors, int expectedErrors)
    {
        await using var provider = BuildProvider(collectAllErrors);
        await using var scope = provider.CreateAsyncScope();
        var first = new Model(false);
        var second = new Model(false);
        var exception = await Assert.ThrowsAsync<ParameterValidationException>(() =>
            scope.ServiceProvider.GetRequiredService<IApplication>()
                .AggregateAsync(first, second, CancellationToken.None));

        Assert.Equal(expectedErrors, exception.Errors.Count);
        Assert.Equal("first", exception.Errors[0].Parameter);
        if (collectAllErrors) Assert.Equal("second", exception.Errors[1].Parameter);
        Assert.Equal(1, first.ValidationCount);
        Assert.Equal(collectAllErrors ? 1 : 0, second.ValidationCount);
        Assert.Equal(Enumerable.Repeat("validate", expectedErrors),
            scope.ServiceProvider.GetRequiredService<CallState>().Events);
    }

    [Fact]
    public async Task CollectAllErrors_IncludesNullModelAndContinuesValidation()
    {
        await using var provider = BuildProvider(true);
        await using var scope = provider.CreateAsyncScope();
        var second = new Model(false);
        var exception = await Assert.ThrowsAsync<ParameterValidationException>(() =>
            scope.ServiceProvider.GetRequiredService<IApplication>().AggregateAsync(null!, second, CancellationToken.None));

        Assert.Equal(new[] { "first", "second" }, exception.Errors.Select(error => error.Parameter));
        Assert.Equal("参数不能为空", exception.Errors[0].Message);
        Assert.Equal(1, second.ValidationCount);
        Assert.Equal(new[] { "validate" }, scope.ServiceProvider.GetRequiredService<CallState>().Events);
    }

    [Fact]
    public async Task CollectAllErrors_StopsOnConfigurationError()
    {
        await using var provider = BuildProvider(true);
        await using var scope = provider.CreateAsyncScope();
        var first = new Model(false);
        var last = new Model(false);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scope.ServiceProvider.GetRequiredService<IApplication>().MixedAsync(first, new InvalidModel(true), last));

        Assert.Equal(1, first.ValidationCount);
        Assert.Equal(0, last.ValidationCount);
        Assert.Equal(new[] { "validate" }, scope.ServiceProvider.GetRequiredService<CallState>().Events);
    }

    [Fact]
    public async Task CollectAllErrors_CancellationStopsValidationAndTransaction()
    {
        await using var provider = BuildProvider(true);
        await using var scope = provider.CreateAsyncScope();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var first = new Model(false);
        var second = new Model(false);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            scope.ServiceProvider.GetRequiredService<IApplication>().AggregateAsync(first, second, cancellation.Token));

        Assert.Equal(0, first.ValidationCount);
        Assert.Equal(0, second.ValidationCount);
        Assert.Empty(scope.ServiceProvider.GetRequiredService<CallState>().Events);
    }

    [Fact]
    public async Task CollectAllErrors_EntersTransactionAfterAllModelsPass()
    {
        await using var provider = BuildProvider(true);
        await using var scope = provider.CreateAsyncScope();
        var first = new Model(true);
        var second = new Model(true);
        await scope.ServiceProvider.GetRequiredService<IApplication>().AggregateAsync(first, second, CancellationToken.None);

        Assert.Equal(new[] { "validate", "validate", "begin", "body", "commit", "transaction-dispose" },
            scope.ServiceProvider.GetRequiredService<CallState>().Events);
    }

    // 以下服务和替身只记录调用顺序, 不连接外部基础设施.
    public interface IApplication
    {
        Task SaveAsync(Model model);
        Task GenericAsync<T>(T model) where T : IAopValidatableObject;
        Task UnconstrainedAsync<T>(T model);
        Task MultipleAsync(string plain, Model first, Model second, CancellationToken token, CancellationToken other);
        Task OtherAsync(object model);
        Task ReuseAsync(ReusableModel model);
        Task NestedAsync(ParentModel model);
        Task InvalidAsync(InvalidModel request);
        Task AggregateAsync(Model first, Model second, CancellationToken token);
        Task MixedAsync(Model first, InvalidModel configuration, Model last);
    }

    public class Application(CallState state) : IApplication
    {
        [Transactional]
        public virtual Task SaveAsync(Model model) => Body();

        public virtual Task GenericAsync<T>(T model) where T : IAopValidatableObject => Body();
        public virtual Task UnconstrainedAsync<T>(T model) => Body();

        public virtual Task MultipleAsync(string plain, Model first, Model second, CancellationToken token,
            CancellationToken other) => Body();

        public virtual Task OtherAsync(object model) => Body();
        public virtual Task ReuseAsync(ReusableModel model) => Body();
        public virtual Task NestedAsync(ParentModel model) => Body();

        [Transactional]
        public virtual Task InvalidAsync(InvalidModel request) => Body();

        [Transactional]
        public virtual Task AggregateAsync(Model first, Model second, CancellationToken token) => Body();

        [Transactional]
        public virtual Task MixedAsync(Model first, InvalidModel configuration, Model last) => Body();

        private Task Body()
        {
            state.Events.Add("body");
            return Task.CompletedTask;
        }
    }

    public record InvalidModel(bool ReturnsNull) : IAopValidatableObject
    {
        public IValidator CreateValidator(IServiceProvider services)
            => ReturnsNull ? null! : new InlineValidator<string>();
    }

    public class Model(bool valid) : IAopValidatableObject
    {
        public int ValidationCount { get; private set; }

        public IValidator CreateValidator(IServiceProvider provider)
            => ModelValidation.Create<Model>(validator => validator.RuleFor(model => model)
                .MustAsync(async (_, token) =>
                {
                    await Task.Yield();
                    token.ThrowIfCancellationRequested();
                    ValidationCount++;
                    provider.GetRequiredService<CallState>().Events.Add("validate");
                    return valid;
                }));
    }

    public interface IPlainService
    {
        Task<string> EchoAsync(string value);
    }

    public class PlainService : IPlainService
    {
        public virtual Task<string> EchoAsync(string value) => Task.FromResult(value);
    }

    public record ReusableModel(string Name) : IAopValidatableObject
    {
        public IValidator CreateValidator(IServiceProvider provider)
            => provider.GetRequiredService<IValidator<ReusableModel>>();
    }

    public class ReusableModelValidator : AbstractValidator<ReusableModel>
    {
        public ReusableModelValidator() => RuleFor(model => model.Name).NotEmpty();

        public int ValidationCount { get; private set; }

        public override Task<ValidationResult> ValidateAsync(ValidationContext<ReusableModel> context,
            CancellationToken cancellationToken = default)
        {
            ValidationCount++;
            return base.ValidateAsync(context, cancellationToken);
        }
    }

    public record ParentModel(ReusableModel Child) : IAopValidatableObject
    {
        public IValidator CreateValidator(IServiceProvider provider)
            => ModelValidation.Create<ParentModel>(validator => validator.RuleFor(model => model.Child)
                .NotNull().SetValidator(ModelValidation.For<ReusableModel>(provider)));
    }

    public class CallState
    {
        public List<string> Events { get; } = [];
    }

    private sealed class TransactionScope(CallState state) : ITransactionScope
    {
        public Task BeginAsync()
        {
            state.Events.Add("begin");
            return Task.CompletedTask;
        }

        public Task CommitAsync()
        {
            state.Events.Add("commit");
            return Task.CompletedTask;
        }

        public Task RollbackAsync()
        {
            state.Events.Add("rollback");
            return Task.CompletedTask;
        }

        public void Dispose() => state.Events.Add("transaction-dispose");
    }

    public class StubProxy : DispatchProxy
    {
        private Func<MethodInfo, object?[]?, object?> _handler = null!;

        public static T Create<T>(Func<MethodInfo, object?[]?, object?> handler) where T : class
        {
            var proxy = DispatchProxy.Create<T, StubProxy>();
            ((StubProxy)(object)proxy)._handler = handler;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? method, object?[]? args) => _handler(method!, args);
    }
}