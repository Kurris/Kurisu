using Kurisu.RemoteCall.Utils;

namespace Kurisu.Test.RemoteCall.UnitTests;

public class CommonUtilsTests
{
    [Fact]
    public void ToObjDictionary_Primitive_ReturnsKeyValue()
    {
        var d = CommonUtils.ToObjDictionary(123, "n");
        Assert.Single(d);
        Assert.True(d.ContainsKey("n"));
        Assert.Equal(123, d["n"]);
    }

    [Fact]
    public void ToObjDictionary_String_ReturnsKeyValue()
    {
        var d = CommonUtils.ToObjDictionary("hello", "s");
        Assert.Single(d);
        Assert.Equal("hello", d["s"].ToString());
    }

    [Fact]
    public void ToObjDictionary_AnonymousObject_FlattensProperties()
    {
        var obj = new { Name = "A", Age = 30 };
        var d = CommonUtils.ToObjDictionary(obj, "root");
        Assert.True(d.ContainsKey("root.Name"));
        Assert.True(d.ContainsKey("root.Age"));
        Assert.Equal("A", d["root.Name"].ToString());
        Assert.Equal("30", d["root.Age"].ToString());
    }

    [Fact]
    public void ToObjDictionary_ListOfMixedObjects_ExpandsIndexedKeys()
    {
        var list = new object[] { new { Name = "X" }, 2, "s" };
        var d = CommonUtils.ToObjDictionary(list, "lst");
        Assert.True(d.ContainsKey("lst[0].Name"));
        Assert.True(d.ContainsKey("lst[1]"));
        Assert.True(d.ContainsKey("lst[2]"));
        Assert.Equal("X", d["lst[0].Name"].ToString());
        Assert.Equal("2", d["lst[1]"].ToString());
        Assert.Equal("s", d["lst[2]"].ToString());
    }

    [Fact]
    public void ToObjDictionary_Dictionary_NestsKeys()
    {
        var dict = new Dictionary<string, object>
        {
            ["k1"] = 1,
            ["k2"] = new { Y = "y" }
        };

        var d = CommonUtils.ToObjDictionary(dict, "root");
        Assert.True(d.ContainsKey("root.k1"));
        Assert.True(d.ContainsKey("root.k2.Y"));
        Assert.Equal("1", d["root.k1"].ToString());
        Assert.Equal("y", d["root.k2.Y"].ToString());
    }

    private class Nested
    {
        public string S { get; set; }
        public List<int> Items { get; set; }
    }

    [Fact]
    public void ToObjDictionary_NestedObjectAndCollection_ProducesIndexedKeys()
    {
        var obj = new { N = new Nested { S = "s", Items = new List<int> { 1, 2 } } };
        var d = CommonUtils.ToObjDictionary(obj, "x");
        Assert.Equal("s", d["x.N.S"].ToString());
        Assert.Equal("1", d["x.N.Items[0]"].ToString());
        Assert.Equal("2", d["x.N.Items[1]"].ToString());
    }
}