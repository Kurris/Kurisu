using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Kurisu.Test.Framework.DependencyInjection.Named.Abstractions;

namespace Kurisu.Test.Framework.DependencyInjection.Named;

[DiInject("dingding")]
public class DingDingSendMessage : ISendMessage
{
    public string Send()
    {
        return "dingding";
    }
}