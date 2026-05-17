using System;
using System.Linq;
using Kurisu.AspNetCore.DependencyInjection;
using Kurisu.Test.Framework.DependencyInjection.Dependencies;
using Kurisu.Test.Framework.DependencyInjection.Dependencies.Abstractions;
using Kurisu.Test.Framework.DependencyInjection.Models;
using Kurisu.Test.Framework.DependencyInjection.Named;
using Kurisu.Test.Framework.DependencyInjection.Named.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Xunit;

namespace Kurisu.Test.Framework.DependencyInjection;

[Trait("di", "loadActiveTypes")]
public class TestLoadActiveTypes
{
    [Fact]
    public void ActiveTypes_Contains_TestProject_DITypes()
    {
        var activeTypes = DependencyInjectionHelper.ActiveTypes.Value;

        Assert.Contains(activeTypes, t => t == typeof(TestSingleton));
        Assert.Contains(activeTypes, t => t == typeof(DingDingSendMessage));
        Assert.Contains(activeTypes, t => t == typeof(WechatSendMessage));
        Assert.Contains(activeTypes, t => t == typeof(EmailSendMessage));
    }

    [Fact]
    public void ActiveTypes_Excludes_SkipScan_Types()
    {
        var activeTypes = DependencyInjectionHelper.ActiveTypes.Value;

        // DependencyInjectionServiceCollectionExtensions 标记了 [SkipScan]，不应出现
        Assert.DoesNotContain(activeTypes, t => t == typeof(DependencyInjectionServiceCollectionExtensions));
    }

    [Fact]
    public void DependencyServices_Contains_TestSingleton()
    {
        var services = DependencyInjectionHelper.DependencyServices.Value;

        Assert.Contains(services, t => t == typeof(TestSingleton));
    }

    [Fact]
    public void DependencyServices_Contains_NamedServices()
    {
        var services = DependencyInjectionHelper.DependencyServices.Value;

        Assert.Contains(services, t => t == typeof(DingDingSendMessage));
        Assert.Contains(services, t => t == typeof(WechatSendMessage));
        Assert.Contains(services, t => t == typeof(EmailSendMessage));
    }

    [Fact]
    public void Configurations_DoesNotThrow()
    {
        // Configurations.Value 不抛异常即通过
        var exception = Record.Exception(() => _ = DependencyInjectionHelper.Configurations.Value);
        Assert.Null(exception);
    }

    [Fact]
    public void ActiveTypes_Returns_Unique_Types()
    {
        var activeTypes = DependencyInjectionHelper.ActiveTypes.Value;
        var distinctCount = activeTypes.Distinct().Count();

        Assert.Equal(distinctCount, activeTypes.Count);
    }

    [Fact]
    public void DependencyContext_Default_Is_Available()
    {
        var context = DependencyContext.Default;

        Assert.NotNull(context);
        Assert.NotEmpty(context.RuntimeLibraries);
    }

    [Fact]
    public void RuntimeLibraries_Contains_Project_References()
    {
        var projects = DependencyContext.Default.RuntimeLibraries
            .Where(lib => string.Equals(lib.Type, "project", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.NotEmpty(projects);
        // 当前测试项目引用 Kurisu.AspNetCore，而 Kurisu.AspNetCore 又引用多个 project
        Assert.Contains(projects, p => p.Name.Contains("Kurisu"));
    }

    [Fact]
    public void DependencyServices_Excludes_Interfaces()
    {
        var services = DependencyInjectionHelper.DependencyServices.Value;

        // 接口不应出现在 DependencyServices 中（IsInterface == false 过滤）
        Assert.DoesNotContain(services, t => t == typeof(ITestSingleton));
        Assert.DoesNotContain(services, t => t == typeof(ISendMessage));
    }

    [Fact]
    public void DependencyServices_Excludes_AbstractClasses()
    {
        var services = DependencyInjectionHelper.DependencyServices.Value;

        // 抽象类不应出现（IsAbstract == false 过滤）
        Assert.DoesNotContain(services, t => t == typeof(Animal));
    }

    [Fact]
    public void ActiveTypes_Is_Lazy_Cached()
    {
        var first = DependencyInjectionHelper.ActiveTypes.Value;
        var second = DependencyInjectionHelper.ActiveTypes.Value;

        Assert.Same(first, second);
    }

    [Fact]
    public void ActiveTypes_Includes_OpenGeneric_DITypes()
    {
        var activeTypes = DependencyInjectionHelper.ActiveTypes.Value;

        // 开放泛型 [DiInject] 类型也应被扫描到
        Assert.Contains(activeTypes, t => t == typeof(GenericsGet<>));
    }
}
