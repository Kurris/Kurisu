using Kurisu.AspNetCore.DependencyInjection.Attributes;
using Kurisu.Test.Framework.DependencyInjection.Named.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.Framework.DependencyInjection.Named;

[DiInject("dingding")]
public class DingDingSendMessage : ISendMessage
{
    public string Send()
    {
        return "dingding";
    }
}