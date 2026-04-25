using Kurisu.AspNetCore.Abstractions.ConfigurableOptions;

namespace Kurisu.Test.Framework.Configurations;

public class TestSetting
{
    public string Name { get; set; }
}


[Configuration("TestSetting")]
public class TestSetting1
{
    public string Name { get; set; }
}