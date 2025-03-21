using Kurisu.Test.Framework.DependencyInjection.Named.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.Framework.DependencyInjection.Named;

[Service("wechat")]
public class WechatSendMessage : ISendMessage
{
    public string Send()
    {
        return "wechat";
    }
}