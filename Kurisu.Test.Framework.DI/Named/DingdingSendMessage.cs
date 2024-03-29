using Kurisu.Test.Framework.DI.Named.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.Framework.DI.Named;

[Service("dingding")]
public class DingDingSendMessage : ISendMessage
{
    public string Send()
    {
        return "dingding";
    }
}