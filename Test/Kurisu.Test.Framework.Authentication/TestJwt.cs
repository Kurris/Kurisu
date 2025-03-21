using System;
using System.Collections.Generic;
using System.Security.Claims;
using Kurisu.AspNetCore.Authentication;
using Kurisu.AspNetCore.Authentication.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Kurisu.Test.Framework.Authentication;

[Trait("Auth", "Jwt")]
public class TestJwt
{
    private readonly IServiceProvider _serviceProvider;

    public TestJwt(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [Fact]
    public void Generate()
    {
        var jwtOptions = _serviceProvider.GetService<IOptions<JwtOptions>>().Value;

        var token = JwtEncryption.GenerateToken(
            new List<Claim>()
            {
                new("sub", "3"),
                new("preferred_username", "ligy"),
                new("userType", "normal"),
                new("code", "DL001")
            },
            new List<Claim>()
            {
            },
            jwtOptions.SecretKey, jwtOptions.Issuer, jwtOptions.Audience, 3600);

        Assert.NotNull(token);

        var user = TestHelper.GetResolver(token);
        Assert.Equal(3, user.GetSubjectId<int>());
        Assert.Equal("ligy", user.GetName());
        Assert.Equal("normal", user.GetUserClaim("userType"));
        Assert.Equal("DL001", user.GetUserClaim("code"));
    }
}