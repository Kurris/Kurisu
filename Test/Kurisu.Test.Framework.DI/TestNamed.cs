using Kurisu.Test.Framework.DI.Named;
using Kurisu.Test.Framework.DI.Named.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kurisu.Test.Framework.DI;

[Trait("di", "dependency")]
public class TestNamed
{
    private readonly INamedResolver _namedResolver;

    public TestNamed(INamedResolver namedResolver)
    {
        _namedResolver = namedResolver;
    }

    [Fact]
    public void GetService_Return_MatchService()
    {
        var dingDingService = _namedResolver.GetService<ISendMessage>("dingding");
        //Assert.Equal(typeof(DingDingSendMessage), dingDingService.GetType());
        Assert.Equal("dingding", dingDingService.Send());

        var wechatService = _namedResolver.GetService<ISendMessage>("wechat");
        Assert.Equal(typeof(WechatSendMessage), wechatService.GetType());
        Assert.Equal("wechat", wechatService.Send());

        var emailService = _namedResolver.GetService<ISendMessage>("email");
        emailService.Send();

        emailService = _namedResolver.GetService<ISendMessage>("email");
        emailService.Send();
        Assert.Equal(typeof(EmailSendMessage), emailService.GetType());
        Assert.Equal("email3", emailService.Send());
    }
}