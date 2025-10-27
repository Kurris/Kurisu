using Kurisu.RemoteCall.Attributes.HelpMethods;
using Xunit;

namespace Kurisu.Test.RemoteCall.ApiTests;

public class PostAttributeBehaviorTests
{
    [Fact]
    public void PostAttribute_DefaultsAndUrlencodedBehavior()
    {
        // 默认构造，contentType 应为 application/json
        var a1 = new PostAttribute("api/test");
        Assert.Equal("application/json", a1.ContentType);
        Assert.False(a1.AsUrlencodedFormat);

        // 指定 asUrlencodedFormat=true 时，不会自动改变 ContentType，但标记会被置为 true
        var a2 = new PostAttribute("api/test", asUrlencodedFormat: true);
        Assert.True(a2.AsUrlencodedFormat);
        Assert.Equal("application/json", a2.ContentType);

        // 指定 contentType 为 form-url-encoded 时，AsUrlencodedFormat 会被强制为 true
        var a3 = new PostAttribute("api/test", contentType: "application/x-www-form-urlencoded", asUrlencodedFormat: false);
        Assert.True(a3.AsUrlencodedFormat);
        Assert.Equal("application/x-www-form-urlencoded", a3.ContentType);

        // 显式指定 contentType 为 multipart，并 asUrlencodedFormat=false
        var a4 = new PostAttribute("api/upload", contentType: "multipart/form-data", asUrlencodedFormat: false);
        Assert.False(a4.AsUrlencodedFormat);
        Assert.Equal("multipart/form-data", a4.ContentType);
    }
}
