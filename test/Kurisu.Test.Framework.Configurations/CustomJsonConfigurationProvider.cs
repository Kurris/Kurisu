using System.Collections.Generic;
using Kurisu.AspNetCore.Abstractions.Startup;

namespace Kurisu.Test.Framework.Configurations;

public class CustomJsonConfigurationProvider : IJsonConfigurationProvider
{
    public int Order => 10;

    public IEnumerable<JsonConfigurationFile> GetJsonFiles(string environmentName)
    {
        yield return new JsonConfigurationFile("custom.json");
        yield return new JsonConfigurationFile($"custom.{environmentName}.json", optional: true);
        yield return new JsonConfigurationFile("custom.missing.json", optional: true);
    }
}
