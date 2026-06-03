using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Kurisu.AspNetCore.Abstractions.ConfigurableOptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Kurisu.Test.Framework.Configurations;

[Trait("configuration", "boundary")]
public class TestAddConfigurationBoundary
{
    /// <summary>
    /// 边界：无[Configuration]类型时，直接返回不抛异常
    /// </summary>
    [Fact]
    public void AddConfiguration_WithNoConfigurationTypes_ReturnsServicesWithoutError()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        // DependencyInjectionHelper.Configurations 懒加载空列表，直接调用方法
        var result = services.AddConfiguration(config);

        Assert.Same(services, result);
    }

    /// <summary>
    /// 边界：配置节存在时正确绑定值
    /// </summary>
    [Fact]
    public void AddConfiguration_WithValidSection_BindsValuesCorrectly()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConfigWithBasicProps"] = "ignored", // 让section存在
                ["ConfigWithBasicProps:Name"] = "test",
                ["ConfigWithBasicProps:Value"] = "42"
            })
            .Build();

        services.AddConfiguration(config);
        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptions<ConfigWithBasicProps>>().Value;
        Assert.Equal("test", options.Name);
        Assert.Equal(42, options.Value);
    }

    /// <summary>
    /// 边界：配置节不存在时使用默认值
    /// </summary>
    [Fact]
    public void AddConfiguration_WithMissingSection_UsesDefaultValues()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        services.AddConfiguration(config);
        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptions<ConfigWithBasicProps>>().Value;
        Assert.Null(options.Name);
        Assert.Equal(0, options.Value);
    }

    /// <summary>
    /// 边界：自定义配置路径[Configuration("custom/path")]
    /// </summary>
    [Fact]
    public void AddConfiguration_WithCustomPath_BindsFromCorrectSection()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["custom:path:Name"] = "custom_name"
            })
            .Build();

        services.AddConfiguration(config);
        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptions<ConfigWithCustomPath>>().Value;
        Assert.Equal("custom_name", options.Name);
    }

    /// <summary>
    /// 边界：[Required]校验 — null值触发OptionsValidationException
    /// </summary>
    [Fact]
    public void AddConfiguration_WithRequiredValidation_ThrowsOnNullValue()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build(); // 无ConfigWithRequired节

        services.AddConfiguration(config);
        var sp = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => sp.GetRequiredService<IOptions<ConfigWithRequired>>().Value);
    }

    /// <summary>
    /// 边界：[Required]校验 — 有效值不抛异常
    /// </summary>
    [Fact]
    public void AddConfiguration_WithRequiredValidation_NoThrowOnValidValue()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConfigWithRequired:Name"] = "valid"
            })
            .Build();

        services.AddConfiguration(config);
        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptions<ConfigWithRequired>>().Value;
        Assert.Equal("valid", options.Name);
    }

    /// <summary>
    /// 边界：[Range]校验 — 超出范围触发OptionsValidationException
    /// </summary>
    [Fact]
    public void AddConfiguration_WithRangeValidation_ThrowsOnOutOfRange()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConfigWithRange:Value"] = "500" // 超出1-100范围
            })
            .Build();

        services.AddConfiguration(config);
        var sp = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => sp.GetRequiredService<IOptions<ConfigWithRange>>().Value);
    }

    /// <summary>
    /// 边界：[Range]校验 — 边界值1有效
    /// </summary>
    [Fact]
    public void AddConfiguration_WithRangeValidation_AcceptsMinValue()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConfigWithRange:Value"] = "1"
            })
            .Build();

        services.AddConfiguration(config);
        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptions<ConfigWithRange>>().Value;
        Assert.Equal(1, options.Value);
    }

    /// <summary>
    /// 边界：[Range]校验 — 边界值100有效
    /// </summary>
    [Fact]
    public void AddConfiguration_WithRangeValidation_AcceptsMaxValue()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConfigWithRange:Value"] = "100"
            })
            .Build();

        services.AddConfiguration(config);
        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptions<ConfigWithRange>>().Value;
        Assert.Equal(100, options.Value);
    }

    /// <summary>
    /// 边界：IValidatableObject自定义校验 — 失败时触发OptionsValidationException
    /// </summary>
    [Fact]
    public void AddConfiguration_WithIValidatableObject_ThrowsOnInvalidState()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build(); // 无节，Start和End均为0

        services.AddConfiguration(config);
        var sp = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => sp.GetRequiredService<IOptions<ConfigWithValidatable>>().Value);
    }

    /// <summary>
    /// 边界：IValidatableObject自定义校验 — 有效数据不抛异常
    /// </summary>
    [Fact]
    public void AddConfiguration_WithIValidatableObject_NoThrowOnValidState()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConfigWithValidatable:Start"] = "10",
                ["ConfigWithValidatable:End"] = "20"
            })
            .Build();

        services.AddConfiguration(config);
        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptions<ConfigWithValidatable>>().Value;
        Assert.Equal(10, options.Start);
        Assert.Equal(20, options.End);
    }

    /// <summary>
    /// 边界：无任何校验属性的类 — 访问Value不抛异常
    /// </summary>
    [Fact]
    public void AddConfiguration_WithNoValidationAttributes_ResolvesWithoutException()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        services.AddConfiguration(config);
        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptions<ConfigWithBasicProps>>().Value;
        Assert.NotNull(options);
    }

    /// <summary>
    /// 边界：实现IStartupConfigure<T>的类型 — StartupConfigure被回调
    /// </summary>
    [Fact]
    public void AddConfiguration_WithIStartupConfigure_CallsStartupConfigure()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConfigWithStartupConfigure:Name"] = "before"
            })
            .Build();

        services.AddConfiguration(config);
        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptions<ConfigWithStartupConfigure>>().Value;
        // StartupConfigure追加 "_configured" 后缀
        Assert.Equal("before_configured", options.Name);
    }

    /// <summary>
    /// 边界：IPostConfigureOptions<T>标记[Configuration]时被移至后置处理，不作为普通Options注册
    /// </summary>
    [Fact]
    public void AddConfiguration_WithIPostConfigureOptions_RegisteredAsPostConfigureSingleton()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        services.AddConfiguration(config);

        // IPostConfigureOptions<ConfigWithBasicProps> 应被注册为可枚举单例
        var sp = services.BuildServiceProvider();
        var postConfigures = sp.GetServices<IPostConfigureOptions<ConfigWithBasicProps>>();

        Assert.Contains(postConfigures, x => x is ConfigAsPostConfigure);
    }

    /// <summary>
    /// 边界：空配置字符串值 — [Required]校验视空字符串为无效
    /// </summary>
    [Fact]
    public void AddConfiguration_WithEmptyStringValue_RequiredValidationThrows()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConfigWithRequired:Name"] = "" // 空字符串
            })
            .Build();

        services.AddConfiguration(config);
        var sp = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => sp.GetRequiredService<IOptions<ConfigWithRequired>>().Value);
    }

    /// <summary>
    /// 边界：多个[Configuration]类型同时注册
    /// </summary>
    [Fact]
    public void AddConfiguration_WithMultipleConfigTypes_AllRegistered()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConfigWithBasicProps:Name"] = "basic",
                ["ConfigWithRequired:Name"] = "required_ok",
                ["ConfigWithRange:Value"] = "50"
            })
            .Build();

        services.AddConfiguration(config);
        var sp = services.BuildServiceProvider();

        var basic = sp.GetRequiredService<IOptions<ConfigWithBasicProps>>().Value;
        var required = sp.GetRequiredService<IOptions<ConfigWithRequired>>().Value;
        var range = sp.GetRequiredService<IOptions<ConfigWithRange>>().Value;

        Assert.Equal("basic", basic.Name);
        Assert.Equal("required_ok", required.Name);
        Assert.Equal(50, range.Value);
    }
}

// ========== 测试用配置类 ==========

/// <summary>
/// 基础配置——无校验属性
/// </summary>
[Configuration]
public class ConfigWithBasicProps
{
    public string Name { get; set; }
    public int Value { get; set; }
}

/// <summary>
/// [Required]校验配置
/// </summary>
[Configuration]
public class ConfigWithRequired
{
    [Required(ErrorMessage = "Name不能为空")]
    public string Name { get; set; }
}

/// <summary>
/// [Range]校验配置
/// </summary>
[Configuration]
public class ConfigWithRange
{
    [Range(1, 100, ErrorMessage = "Value必须在1到100之间")]
    public int Value { get; set; }
}

/// <summary>
/// 自定义路径配置
/// </summary>
[Configuration("custom:path")]
public class ConfigWithCustomPath
{
    public string Name { get; set; }
}

/// <summary>
/// IValidatableObject自定义校验配置
/// </summary>
[Configuration]
public class ConfigWithValidatable : IValidatableObject
{
    public int Start { get; set; }
    public int End { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Start >= End)
            yield return new ValidationResult("Start必须小于End", [nameof(Start), nameof(End)]);
    }
}

/// <summary>
/// IStartupConfigure启动配置——绑定时回调
/// </summary>
[Configuration]
public class ConfigWithStartupConfigure : IStartupConfigure<ConfigWithStartupConfigure>
{
    public string Name { get; set; }

    public void StartupConfigure(ConfigWithStartupConfigure value)
    {
        value.Name += "_configured";
    }
}

/// <summary>
/// IPostConfigureOptions后置处理——标记[Configuration]验证正确分流
/// </summary>
[Configuration]
public class ConfigAsPostConfigure : IPostConfigureOptions<ConfigWithBasicProps>
{
    public void PostConfigure(string name, ConfigWithBasicProps options)
    {
    }
}
