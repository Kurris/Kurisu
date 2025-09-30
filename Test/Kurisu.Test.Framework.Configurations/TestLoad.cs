using System;
using Kurisu.AspNetCore.Authentication.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Kurisu.Test.Framework.Configurations;

[Trait("configuration", "load")]
public class TestLoad
{
    private readonly IServiceProvider _serviceProvider;

    public TestLoad(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }


    [Fact]
    public void QueryDbConnectionString_return_exists()
    {
        // var dbOptions = _serviceProvider.GetService<IOptions<DbSetting>>();
        // var dbSetting = dbOptions.Value;
        //
        // Assert.NotNull(dbSetting);
        // Assert.Single(dbSetting.ReadConnectionStrings);
        // Assert.NotEmpty(dbSetting.DefaultConnectionString);
    }

    // [Fact]
    // public void QueryIdentityServer4Setting_return_exists_And_Equals()
    // {
    //     var identityOptions = _serviceProvider.GetService<IOptions<IdentityServerOptions>>();
    //     var options = _serviceProvider.GetService<IOptions<PatOptions>>();
    //
    //     //Assert.Throws<OptionsValidationException>(() => identityOptions.Value);
    //     var identitySetting = identityOptions.Value;
    //
    //     Assert.NotNull(identitySetting);
    //     Assert.Equal("https://isawesome.cn:5000", identitySetting.Authority);
    // }

    [Fact]
    public void QueryTestSetting_WithNoConfigurationAttribute_Return_Null()
    {
        //没有【ConfigurationAttribute】
        var testOptions = _serviceProvider.GetService<IOptions<TestSetting>>();

        var testSetting = testOptions.Value;

        Assert.Null(testSetting.Name);
    }

    [Fact]
    public void QueryTestSetting_WithConfigurationAttribute_WithSpecialName_Return_Null()
    {
        //有【ConfigurationAttribute】
        var testOptions = _serviceProvider.GetService<IOptions<TestSetting1>>();

        var testSetting = testOptions.Value;

        Assert.Null(testSetting.Name);
    }
}
