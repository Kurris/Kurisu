using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AspectCore.DynamicProxy;
using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kurisu.Test.Framework.DependencyInjection;

public class TestProxy
{

    [Fact]
    public void Test_AbstractProxy()
    {
        var sp = TestHelper.GetServiceProvider();
        //var a = sp.GetKeyedService<AbstractProxy>("ProxyImpl");
       // a.GetName();
    }
}


public abstract class AbstractProxy
{

    [AopClass]
    public virtual string GetName()
    {
        return "AbstractProxy";
    }
}


[DiInject("ProxyImpl")]
public class ProxyImpl : AbstractProxy
{

}

[AttributeUsage(AttributeTargets.Method)]
public class AopClass : AopAttribute
{
    public override async Task Invoke(AspectContext context, AspectDelegate next)
    {
        await next(context);
    }
}
