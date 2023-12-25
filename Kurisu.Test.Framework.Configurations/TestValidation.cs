using System;
using Kurisu.AspNetCore.Document.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Kurisu.Test.Framework.Configurations;

[Trait("configuration", "Validation")]
public class TestValidation
{
    private readonly IServiceProvider _serviceProvider;

    public TestValidation(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [Fact]
    public void QuerySwaggerSetting_WithNoClientId_Return_OptionsValidationException()
    {
        var swaggerOptions = _serviceProvider.GetService<IOptions<SwaggerOptions>>();
        Assert.Throws<OptionsValidationException>(() => { _ = swaggerOptions.Value; });
    }
}