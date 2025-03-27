using Kurisu.AspNetCore.Cache;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Kurisu.Test.DataProtection;

[Trait("dataProtection", "dataProtection")]
public class TestDataProtection
{
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly RedisCache _redisCache;
    private readonly Kurisu.AspNetCore.DataProtection.Settings.DataProtectionOptions _dataProtectionOptions;

    public TestDataProtection(IDataProtectionProvider dataProtectionProvider, RedisCache redisCache, IOptions<Kurisu.AspNetCore.DataProtection.Settings.DataProtectionOptions> options)
    {
        _dataProtectionProvider = dataProtectionProvider;
        _redisCache = redisCache;
        _dataProtectionOptions = options.Value;
    }


    [Fact]
    public void Protection()
    {
        var protector = _dataProtectionProvider.CreateProtector("test");
        var encryptText = protector.Protect("123");
        var decryptText = protector.Unprotect(encryptText);

        Assert.Equal("123", decryptText);
        
        var list = _redisCache.ListRange(_dataProtectionOptions.Key);
        Assert.NotEmpty(list);
    }
}