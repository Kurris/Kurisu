using Kurisu.Extensions.FluentValidation.Abstractions;
using Kurisu.Extensions.FluentValidation.Result;
using FluentValidation;
using Kurisu.Extensions.FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kurisu.Test.FluentValidation;

public class ModelValidationTests
{
    [Fact]
    public async Task NestedCollection_PreservesRootDataPathsAndFailureMetadata()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var child = new Child();
        var validator = ModelValidation.Create<Parent>(rules =>
            rules.RuleForEach(x => x.Children).SetValidator(ModelValidation.For<Child>(services)));
        var context = new ValidationContext<Parent>(new Parent([child]));
        context.RootContextData["message"] = "shared";

        var result = await validator.ValidateAsync(context);

        var error = Assert.Single(result.Errors);
        Assert.Equal("Children[0].Name", error.PropertyName);
        Assert.Equal("shared", error.ErrorMessage);
        Assert.Equal("child-code", error.ErrorCode);
        Assert.Equal(Severity.Warning, error.Severity);
        Assert.Same(child, error.CustomState);
    }

    [Fact]
    public async Task BaseDeclaredChild_UsesRuntimeDerivedValidator()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var validator = ModelValidation.Create<BaseParent>(rules =>
            rules.RuleFor(x => x.Child).SetValidator(ModelValidation.For<BaseChild>(services)));

        var result = await validator.ValidateAsync(new BaseParent(new DerivedChild()));

        Assert.Equal("Child.Name", Assert.Single(result.Errors).PropertyName);
    }

    [Fact]
    public void SynchronousChild_CanValidateSynchronously()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var validator = ModelValidation.For<BaseChild>(services);
        Assert.False(validator.Validate(new DerivedChild()).IsValid);
    }

    [Fact]
    public async Task CancelledChild_DoesNotCreateValidator()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var child = new Child();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ModelValidation.For<Child>(services).ValidateAsync(child, cancellation.Token));
        Assert.Equal(0, child.Created);
    }

    [Theory]
    [InlineData(true, "未提供有效的验证器")]
    [InlineData(false, "不支持模型类型")]
    public async Task NestedInvalidValidator_ReportsConfigurationError(bool returnsNull, string message)
    {
        await using var services = new ServiceCollection().BuildServiceProvider();
        var childValidator = ModelValidation.For<InvalidChild>(services);
        var validator = ModelValidation.Create<InvalidParent>(rules =>
            rules.RuleFor(x => x.Model).SetValidator(childValidator));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            validator.ValidateAsync(new InvalidParent(new InvalidChild(returnsNull))));

        Assert.Contains(nameof(InvalidChild), exception.Message);
        Assert.Contains(message, exception.Message);
    }

    [Fact]
    public async Task NestedValidator_RespectsSelectedRuleSet()
    {
        await using var services = new ServiceCollection().BuildServiceProvider();
        var childValidator = ModelValidation.For<RuleSetChild>(services);
        var validator = ModelValidation.Create<RuleSetParent>(rules => rules.RuleSet("selected", () =>
            rules.RuleFor(x => x.Model).SetValidator(childValidator)));

        var result = await validator.ValidateAsync(new RuleSetParent(new RuleSetChild()),
            options => options.IncludeRuleSets("selected"));

        Assert.Equal("Model.Name", Assert.Single(result.Errors).PropertyName);
    }

    public record InvalidParent(InvalidChild Model);

    public record InvalidChild(bool ReturnsNull) : IAopValidatableObject
    {
        public IValidator CreateValidator(IServiceProvider services)
            => ReturnsNull ? null! : new InlineValidator<string>();
    }

    public record RuleSetParent(RuleSetChild Model);

    public class RuleSetChild : IAopValidatableObject
    {
        public string Name => "";
        public string Other => "";

        public IValidator CreateValidator(IServiceProvider services) => ModelValidation.Create<RuleSetChild>(rules =>
        {
            rules.RuleFor(x => x.Other).NotEmpty();
            rules.RuleSet("selected", () => rules.RuleFor(x => x.Name).NotEmpty());
        });
    }

    public record Parent(Child[] Children);

    public record BaseParent(BaseChild Child);

    public class Child : IAopValidatableObject
    {
        public string Name { get; } = "";
        public int Created { get; private set; }

        public IValidator CreateValidator(IServiceProvider services)
        {
            Created++;
            return ModelValidation.Create<Child>(rules => rules.RuleFor(x => x.Name)
                .Must((model, name, context) => false)
                .WithMessage((model, name) => "unused")
                .WithErrorCode("child-code").WithSeverity(Severity.Warning).WithState(x => x)
                .Configure(rule => rule.MessageBuilder = context =>
                    (string)context.ParentContext.RootContextData["message"]));
        }
    }

    public abstract class BaseChild : IAopValidatableObject
    {
        public abstract IValidator CreateValidator(IServiceProvider services);
    }

    public class DerivedChild : BaseChild
    {
        public string Name { get; } = "";

        public override IValidator CreateValidator(IServiceProvider services) =>
            ModelValidation.Create<DerivedChild>(rules => rules.RuleFor(x => x.Name).NotEmpty());
    }
}