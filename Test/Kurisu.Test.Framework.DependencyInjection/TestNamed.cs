using Kurisu.Test.Framework.DependencyInjection.Named;
using Kurisu.Test.Framework.DependencyInjection.Named.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kurisu.Test.Framework.DependencyInjection;

[Trait("di", "dependency")]
public class TestNamed
{
    [Fact]
    public void GetService_Return_MatchService()
    {
        var sp = TestHelper.GetServiceProvider();
        using (var scope = sp.CreateScope())
        {
            INamedResolver namedResolver = scope.ServiceProvider.GetService<INamedResolver>();
            var dingDingService = namedResolver.GetService<ISendMessage>("dingding");
            //Assert.Equal(typeof(DingDingSendMessage), dingDingService.GetType());
            Assert.Equal("dingding", dingDingService.Send());

            var wechatService = namedResolver.GetService<ISendMessage>("wechat");
            Assert.Equal(typeof(WechatSendMessage), wechatService.GetType());
            Assert.Equal("wechat", wechatService.Send());

            var emailService = namedResolver.GetService<ISendMessage>("email");
            emailService.Send();

            emailService = namedResolver.GetService<ISendMessage>("email");
            emailService.Send();
            Assert.Equal(typeof(EmailSendMessage), emailService.GetType());
            Assert.Equal("email3", emailService.Send());
        }
    }
}