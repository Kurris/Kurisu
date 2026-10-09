using Kurisu.Extensions.FluentValidation.Abstractions;
using Kurisu.Extensions.FluentValidation.Result;
using FluentValidation;
using Kurisu.Extensions.FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kurisu.Test.FluentValidation;

public class ExplicitModelValidationTests
{
    [Fact]
    public async Task ValidModel_ReusesValidatorWithinScope()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var model = new Request("valid");
        var validator = model.CreateValidator(scope.ServiceProvider);

        await ModelValidation.ValidateAsync(model, scope.ServiceProvider);
        await ModelValidation.ValidateAsync(model, scope.ServiceProvider);

        Assert.Equal(2, scope.ServiceProvider.GetRequiredService<ValidationState>().Validated);
        Assert.Same(validator, model.CreateValidator(scope.ServiceProvider));
        await using var otherScope = provider.CreateAsyncScope();
        Assert.NotSame(validator, model.CreateValidator(otherScope.ServiceProvider));
        Assert.Equal(0, otherScope.ServiceProvider.GetRequiredService<ValidationState>().Validated);
    }

    [Fact]
    public async Task InvalidModel_ReportsBusinessMessageWithoutProxyRegistration()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var exception = await Assert.ThrowsAsync<ParameterValidationException>(() =>
            ModelValidation.ValidateAsync(new Request(""), scope.ServiceProvider));

        var error = Assert.Single(exception.Errors);
        Assert.Equal("model", error.Parameter);
        Assert.Equal("Name", error.Field);
        Assert.Equal("biz_user_login_fail", error.Message);
    }

    [Fact]
    public async Task NullModel_ReportsParameterError()
    {
        await using var provider = CreateProvider();
        var exception = await Assert.ThrowsAsync<ParameterValidationException>(() =>
            ModelValidation.ValidateAsync<Request>(null!, provider));
        var error = Assert.Single(exception.Errors);
        Assert.Equal("model", error.Parameter);
        Assert.Equal("model", error.Field);
        Assert.Equal("参数不能为空", error.Message);
    }

    [Fact]
    public async Task NestedModel_PreservesPropertyPath()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var exception = await Assert.ThrowsAsync<ParameterValidationException>(() =>
            ModelValidation.ValidateAsync(new Command(new Request("")), scope.ServiceProvider));

        Assert.Equal("Request.Name", Assert.Single(exception.Errors).Field);
    }

    [Fact]
    public async Task AsyncRule_ReceivesCancellationToken()
    {
        await using var provider = CreateProvider();
        using var cancellation = new CancellationTokenSource();
        var model = new TokenRequest(cancellation.Token);
        await ModelValidation.ValidateAsync(model, provider, cancellation.Token);
        Assert.True(model.TokenReceived);
    }

    [Fact]
    public async Task CancelledCall_DoesNotCreateValidator()
    {
        await using var provider = CreateProvider();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var model = new TokenRequest(cancellation.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ModelValidation.ValidateAsync(model, provider, cancellation.Token));
        Assert.False(model.ValidatorCreated);
    }

    private static ServiceProvider CreateProvider() => new ServiceCollection()
        .AddScoped<ValidationState>()
        .AddScoped<IValidator<Request>, RequestValidator>()
        .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

    public class ValidationState
    {
        public int Validated { get; set; }
    }

    public record Request(string Name) : IAopValidatableObject
    {
        public IValidator CreateValidator(IServiceProvider services) => services.GetRequiredService<IValidator<Request>>();
    }

    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator(ValidationState state) => RuleFor(x => x.Name).MustAsync((name, _) =>
        {
            state.Validated++;
            return Task.FromResult(!string.IsNullOrWhiteSpace(name));
        }).WithMessage("biz_user_login_fail");
    }

    public record Command(Request Request) : IAopValidatableObject
    {
        public IValidator CreateValidator(IServiceProvider services)
        {
            var childValidator = ModelValidation.For<Request>(services);
            return ModelValidation.Create<Command>(rules => rules.RuleFor(x => x.Request).SetValidator(childValidator));
        }
    }

    public class TokenRequest(CancellationToken expectedToken) : IAopValidatableObject
    {
        public bool TokenReceived { get; private set; }
        public bool ValidatorCreated { get; private set; }

        public IValidator CreateValidator(IServiceProvider services)
        {
            ValidatorCreated = true;
            return ModelValidation.Create<TokenRequest>(rules => rules.RuleFor(x => x).MustAsync(async (_, token) =>
            {
                await Task.Yield();
                TokenReceived = token == expectedToken;
                return TokenReceived;
            }));
        }
    }
}
