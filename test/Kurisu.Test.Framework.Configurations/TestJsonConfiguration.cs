using System;
using System.IO;
using Kurisu.AspNetCore.Abstractions.Startup;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Kurisu.Test.Framework.Configurations;

[Trait("configuration", "json")]
public class TestJsonConfiguration
{
    [Fact]
    public void AddKurisuJsonConfigurationFiles_WithEnvironment_LoadsCustomJsonFiles()
    {
        var configuration = new ConfigurationBuilder()
            .AddKurisuJsonConfigurationFiles("Development")
            .Build();

        Assert.Equal("Development", configuration["KurisuJson:Environment"]);
        Assert.Equal("custom.Development.json", configuration["KurisuJson:Source"]);
        Assert.Equal("true", configuration["KurisuJson:OptionalMissingFileIgnored"]);
    }

    [Fact]
    public void AddKurisuJsonConfigurationFiles_WithCustomBasePath_LoadsFilesFromOutputDirectory()
    {
        var basePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(basePath);

        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddKurisuJsonConfigurationFiles("Development")
                .Build();

            Assert.Equal("true", configuration["KurisuJsonOutputOnly:Loaded"]);
        }
        finally
        {
            Directory.Delete(basePath, recursive: true);
        }
    }

    [Fact]
    public void JsonConfigurationFile_WithEmptyPath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new JsonConfigurationFile(""));
    }

    [Fact]
    public void JsonConfigurationFile_WithNonJsonPath_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => new JsonConfigurationFile("custom.txt"));
    }
}
