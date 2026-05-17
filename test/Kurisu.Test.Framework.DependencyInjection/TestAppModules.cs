using System.Linq;
using Kurisu.AspNetCore;
using Kurisu.Test.Framework.DependencyInjection.Modules;
using Xunit;

namespace Kurisu.Test.Framework.DependencyInjection;

[Trait("di", "appModules")]
public class TestAppModules
{
    [Fact]
    public void Modules_Only_Contains_MostDerived_Type()
    {
        var modules = App.Modules.Value;

        // 基类模块被排除
        Assert.DoesNotContain(modules, m => m.GetType() == typeof(TestHealthCheckModule));

        // 最派生模块保留
        Assert.Contains(modules, m => m.GetType() == typeof(CustomHealthCheckModule));
    }

    [Fact]
    public void Modules_CustomModule_Overrides_Name()
    {
        var customModule = App.Modules.Value.OfType<CustomHealthCheckModule>().Single();

        Assert.Equal("自定义健康检查模块", customModule.Name);
    }

    [Fact]
    public void Modules_All_Are_Concrete_NonAbstract()
    {
        foreach (var module in App.Modules.Value)
        {
            var type = module.GetType();
            Assert.True(type.IsClass);
            Assert.False(type.IsAbstract);
        }
    }

    [Fact]
    public void Modules_Respects_Order_Descending()
    {
        var modules = App.Modules.Value.ToList();
        var ordered = modules.OrderBy(m => m.Order).ToList();

        Assert.Equal(ordered.Select(m => m.Order), modules.Select(m => m.Order));
    }
}
