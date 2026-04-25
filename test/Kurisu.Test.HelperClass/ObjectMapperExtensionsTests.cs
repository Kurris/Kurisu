using Kurisu.AspNetCore.Abstractions.ObjectMapper;
using Xunit;

namespace Kurisu.Test.HelperClass;

public class ObjectMapperExtensionsTests
{
    private class Src
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string? Optional { get; set; }
        public string Extra { get; set; }
    }

    private class Dest
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string? Optional { get; set; }
        public string Custom { get; set; }
    }

    [Fact]
    public void Map_CopiesProperties()
    {
        var src = new Src { Name = "Alice", Age = 30, Optional = "o", Extra = "e" };
        var dest = src.Map<Src, Dest>();
        Assert.Equal("Alice", dest.Name);
        Assert.Equal(30, dest.Age);
        Assert.Equal("o", dest.Optional);
    }

    [Fact]
    public void Map_WithPropertyMappings_SetsByFactory()
    {
        var src = new Src { Name = "C", Age = 5, Extra = "ex" };
        var dest = src.Map<Src, Dest>((d => d.Custom, s => s.Name + "-mapped"), (d => d.Name, s => s.Extra));
        Assert.Equal("ex", dest.Name);
        Assert.Equal("C-mapped", dest.Custom);
    }

    [Fact]
    public void Map_PropertyFactoryThrows_IsIgnored()
    {
        var src = new Src { Name = "X", Age = 1 };
        // valueFactory will throw and should propagate the exception according to implementation
        Assert.Throws<System.InvalidOperationException>(() => src.Map<Src, Dest>((d => d.Custom, s => throw new System.InvalidOperationException("boom"))));
    }

    [Fact]
    public void Map_ConversionFromIntToString_Works()
    {
        var src = new Src { Name = "N", Age = 42 };
        var dest = src.Map<Src, Dest>((d => d.Custom, s => s.Age));
        // Age converted to string
        Assert.Equal("42", dest.Custom);
    }

    [Fact]
    public void MapTo_UpdatesDestination()
    {
        var src = new Src { Name = "U", Age = 9, Optional = "opt" };
        var dest = new Dest { Name = "old", Age = 1, Optional = null, Custom = "c" };
        var res = src.MapTo(dest);
        Assert.Equal("U", dest.Name);
        Assert.Equal(9, dest.Age);
        Assert.Equal("opt", dest.Optional);
    }

    [Fact]
    public void MapToIgnore_IgnoresProperties()
    {
        var src = new Src { Name = "I", Age = 11 };
        var dest = new Dest { Name = "old", Age = 2 };
        var res = src.MapToIgnore(dest, d => d.Age);
        Assert.Equal("I", dest.Name);
        Assert.Equal(2, dest.Age);
    }

    [Fact]
    public void MapToIgnoreNull_DoesNotOverwriteWithNull()
    {
        var src = new Src { Name = "N", Optional = null };
        var dest = new Dest { Name = "old", Optional = "keep" };
        var res = src.MapToIgnoreNull(dest);
        Assert.Equal("N", dest.Name);
        Assert.Equal("keep", dest.Optional);
    }
}
