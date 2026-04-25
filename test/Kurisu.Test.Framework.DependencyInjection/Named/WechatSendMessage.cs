using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Kurisu.Test.Framework.DependencyInjection.Named.Abstractions;

namespace Kurisu.Test.Framework.DependencyInjection.Named;

[DiInject("wechat")]
public class WechatSendMessage : ISendMessage
{
    public string Send()
    {
        return "wechat";
    }
}