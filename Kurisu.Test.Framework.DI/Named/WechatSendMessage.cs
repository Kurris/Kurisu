using Kurisu.Test.Framework.DI.Named.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.Framework.DI.Named
{
    [Register("wechat")]
    public class WechatSendMessage : ISendMessage
    {
        public string Send()
        {
            return "wechat";
        }
    }
}