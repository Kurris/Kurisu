using Kurisu.RemoteCall.Utils;
using Kurisu.Test.RemoteCall.TestHelpers;
using System.Collections;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.RemoteCall.UnitTests;

public class CommonUtilsTests : IClassFixture<RemoteCallTestFixture>
{
    private readonly RemoteCallTestFixture _fixture;
    public CommonUtilsTests(RemoteCallTestFixture fixture) => _fixture = fixture;

    [Fact]
    public void ToObjDictionary_Primitive_ReturnsKeyValue()
    {
        var d = _fixture.GetService<ICommonUtils>().ToObjDictionary("n", 123);
        Assert.Single(d);
        Assert.True(d.ContainsKey("n"));
        Assert.Equal(123, d["n"]);
    }

    [Fact]
    public void ToObjDictionary_String_ReturnsKeyValue()
    {
        var d = _fixture.GetService<ICommonUtils>().ToObjDictionary("s", "hello");
        Assert.Single(d);
        Assert.Equal("hello", d["s"].ToString());
    }

    [Fact]
    public void ToObjDictionary_AnonymousObject_FlattensProperties()
    {
        var obj = new { Name = "A", Age = 30 };
        var d = _fixture.GetService<ICommonUtils>().ToObjDictionary("root", obj);
        Assert.True(d.ContainsKey("root.Name"));
        Assert.True(d.ContainsKey("root.Age"));
        Assert.Equal("A", d["root.Name"].ToString());
        Assert.Equal("30", d["root.Age"].ToString());
    }

    [Fact]
    public void ToObjDictionary_ListOfMixedObjects_ExpandsIndexedKeys()
    {
        var list = new object[] { new { Name = "X" }, 2, "s" };
        var d = _fixture.GetService<ICommonUtils>().ToObjDictionary("lst", list);
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

        var d = _fixture.GetService<ICommonUtils>().ToObjDictionary("root", dict);
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
        var d = _fixture.GetService<ICommonUtils>().ToObjDictionary("x", obj);
        Assert.Equal("s", d["x.N.S"].ToString());
        Assert.Equal("1", d["x.N.Items[0]"].ToString());
        Assert.Equal("2", d["x.N.Items[1]"].ToString());
    }

    [Fact]
    public void ToObjDictionary_Null_Throws()
    {
        var utils = _fixture.GetService<ICommonUtils>();
        Assert.ThrowsAny<Exception>(() => utils.ToObjDictionary("p", null));
    }

    [Fact]
    public void ToObjDictionary_EmptyEnumerable_SetsPrefixToNull()
    {
        var utils = _fixture.GetService<ICommonUtils>();
        var empty = new List<int>();
        var d = utils.ToObjDictionary("arr", empty);
        Assert.True(d.ContainsKey("arr"));
        Assert.Null(d["arr"]);
    }

    [Fact]
    public void ToObjDictionary_ByteArray_HandledAsSimpleType()
    {
        var utils = _fixture.GetService<ICommonUtils>();
        var bytes = "hello"u8.ToArray();
        var d = utils.ToObjDictionary("b", bytes);
        Assert.Single(d);
        Assert.True(d.ContainsKey("b"));
        Assert.IsType<byte[]>(d["b"]);
        Assert.Equal(bytes, (byte[])d["b"]);
    }

    [Fact]
    public void ToObjDictionary_JToken_ObjectAndArray()
    {
        var utils = _fixture.GetService<ICommonUtils>();
        var j = JObject.Parse("{ \"a\": 1, \"arr\": [ { \"x\": 5 }, 2 ] }");
        var d = utils.ToObjDictionary("j", j);
        Assert.Equal("1", d["j.a"].ToString());
        Assert.Equal("5", d["j.arr[0].x"].ToString());
        Assert.Equal("2", d["j.arr[1]"].ToString());
    }

    [Fact]
    public void ToObjDictionary_NonGenericDictionary_HasKeys()
    {
        var utils = _fixture.GetService<ICommonUtils>();
        var ht = new Hashtable();
        ht["one"] = 1;
        ht["two"] = new { Z = "z" };
        var d = utils.ToObjDictionary("h", ht);
        Assert.Equal("1", d["h.one"].ToString());
        Assert.Equal("z", d["h.two.Z"].ToString());
    }

    [Fact]
    public void ToObjDictionary_GenericDictionary_NonStringKey()
    {
        var utils = _fixture.GetService<ICommonUtils>();
        var gd = new Dictionary<int, object>();
        gd[1] = new { A = 10 };
        var d = utils.ToObjDictionary("g", gd);
        Assert.Equal("10", d["g.1.A"].ToString());
    }

    [Fact]
    public void IsReferenceType_Behavior()
    {
        var utils = _fixture.GetService<ICommonUtils>();
        Assert.False(utils.IsReferenceType(typeof(string)));
        Assert.True(utils.IsReferenceType(typeof(object)));
        Assert.False(utils.IsReferenceType(typeof(int)));
        Assert.True(utils.IsReferenceType(typeof(int[])));
    }

    [Fact]
    public void ToObjDictionary_JValue_MapsToPrefix()
    {
        var utils = _fixture.GetService<ICommonUtils>();
        var jv = new JValue(42);
        var d = utils.ToObjDictionary("v", jv);
        Assert.Equal(42, Convert.ToInt32(d["v"]));
    }

    [Fact]
    public void ToObjDictionary_JArrayEmpty_SetsPrefixNull()
    {
        var utils = _fixture.GetService<ICommonUtils>();
        var ja = JArray.Parse("[]");
        var d = utils.ToObjDictionary("ja", ja);
        Assert.True(d.ContainsKey("ja"));
        Assert.Null(d["ja"]);
    }

    [Fact]
    public void ToObjDictionary_SimpleType_EmptyPrefix_Throws()
    {
        var utils = _fixture.GetService<ICommonUtils>();
        Assert.Throws<ArgumentException>(() => utils.ToObjDictionary(string.Empty, 1));
    }

    private class Poco
    {
        public int A { get; set; }
        public Nested Nested { get; set; }
    }

    [Fact]
    public void ToObjDictionary_Poco_UsesSerializerPath()
    {
        var utils = _fixture.GetService<ICommonUtils>();
        var p = new Poco { A = 7, Nested = new Nested { S = "ok", Items = new List<int> { 9 } } };
        var d = utils.ToObjDictionary("p", p);
        // either path (JustObject or reflection) should lead to these keys
        Assert.Equal("7", d["p.A"].ToString());
        Assert.Equal("ok", d["p.Nested.S"].ToString());
        Assert.Equal("9", d["p.Nested.Items[0]"].ToString());
    }

    [Fact]
    public void ToObjDictionary_GenericDictionary_GuidKey()
    {
        var utils = _fixture.GetService<ICommonUtils>();
        var g = Guid.NewGuid();
        var dict = new Dictionary<Guid, object> { [g] = new { V = "x" } };
        var d = utils.ToObjDictionary("g", dict);
        Assert.Equal("x", d[$"g.{g}.V"].ToString());
    }

    [Fact]
    public void Decorator_CanWrapAndModifyBehavior()
    {
        // Build a fresh service collection but then replace ICommonUtils with a decorated implementation
        var services = new ServiceCollection();
        services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        // register remote call same as fixture (only need one interface to trigger registrations)
        services.AddRemoteCall(new[] { typeof(Kurisu.Test.RemoteCall.Api.IGetApi) });

        var origDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(ICommonUtils));
        Assert.NotNull(origDescriptor);
        services.Remove(origDescriptor!);

        var commonUtilsType = typeof(ICommonUtils).Assembly.GetType("Kurisu.RemoteCall.Utils.CommonUtils");
        Assert.NotNull(commonUtilsType);

        services.AddSingleton<ICommonUtils>(sp =>
        {
            var original = (ICommonUtils)ActivatorUtilities.CreateInstance(sp, commonUtilsType!);
            return new DecoratedCommonUtils(original);
        });

        var provider = services.BuildServiceProvider();
        var decorated = provider.GetRequiredService<ICommonUtils>();

        var d = decorated.ToObjDictionary("p", new { V = 1 });
        Assert.Equal("decorated:1", d["p.V"].ToString());
    }

    private class DecoratedCommonUtils : ICommonUtils
    {
        private readonly ICommonUtils _inner;
        public DecoratedCommonUtils(ICommonUtils inner) => _inner = inner;
        public bool IsReferenceType(Type type) => _inner.IsReferenceType(type);
        public Dictionary<string, object> ToObjDictionary(string prefix, object obj)
        {
            var d = _inner.ToObjDictionary(prefix, obj);
            var keys = d.Keys.ToList();
            foreach (var k in keys)
            {
                if (d[k] != null && d[k].GetType() != typeof(JToken))
                {
                    d[k] = $"decorated:{d[k]}";
                }
            }

            return d;
        }
    }

    [Fact]
    public void ToObjDictionary_Enumerable_PrefixNull_ProducesBracketKeys()
    {
        var utils = _fixture.GetService<ICommonUtils>();
        var arr = new object[] { new { Name = "Root" }, 5 };
        var d = utils.ToObjDictionary(null, arr);
        Assert.True(d.ContainsKey("[0].Name"));
        Assert.True(d.ContainsKey("[1]"));
        Assert.Equal("Root", d["[0].Name"].ToString());
        Assert.Equal("5", d["[1]"].ToString());
    }

    [Fact]
    public void ToObjDictionary_EmptyEnumerable_PrefixNull_ReturnsEmpty()
    {
        var utils = _fixture.GetService<ICommonUtils>();
        var empty = new List<string>();
        var d = utils.ToObjDictionary(null, empty);
        Assert.Empty(d);
    }

    [Fact]
    public void ToObjDictionary_Poco_NullProperty_IncludedAsNull()
    {
        var utils = _fixture.GetService<ICommonUtils>();
        var p = new Poco { A = 1, Nested = null };
        var d = utils.ToObjDictionary("p", p);
        Assert.True(d.ContainsKey("p.Nested"));
        Assert.Null(d["p.Nested"]);
    }

    [Fact]
    public void ToObjDictionary_SimpleType_NullPrefix_Throws()
    {
        var utils = _fixture.GetService<ICommonUtils>();
        Assert.Throws<ArgumentException>(() => utils.ToObjDictionary(null, 123));
    }

    [Fact]
    public void ToObjDictionary_NonGenericDictionary_NonStringKey()
    {
        var utils = _fixture.GetService<ICommonUtils>();
        var ht = new Hashtable();
        ht[1] = "one";
        var d = utils.ToObjDictionary("h", ht);
        Assert.Equal("one", d["h.1"].ToString());
    }

    [Fact]
    public void ToObjDictionary_ReflectionPath_WhenSerializerThrows()
    {
        // Build a fresh service collection and replace IJsonSerializer with one that throws
        var services = new ServiceCollection();
        services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        services.AddRemoteCall(new[] { typeof(Kurisu.Test.RemoteCall.Api.IGetApi) });

        // replace IJsonSerializer with throwing implementation
        var orig = services.FirstOrDefault(sd => sd.ServiceType == typeof(Kurisu.RemoteCall.Abstractions.IJsonSerializer));
        Assert.NotNull(orig);
        services.Remove(orig!);
        services.AddSingleton<Kurisu.RemoteCall.Abstractions.IJsonSerializer, ThrowingSerializer>();

        var provider = services.BuildServiceProvider();
        var utils = provider.GetRequiredService<ICommonUtils>();

        var p = new { A = 5, B = new { C = 6 } };
        var d = utils.ToObjDictionary("r", p);
        // reflection path should produce these keys
        Assert.Equal("5", d["r.A"].ToString());
        Assert.Equal("6", d["r.B.C"].ToString());
    }

    private class ThrowingSerializer : Kurisu.RemoteCall.Abstractions.IJsonSerializer
    {
        public T Deserialize<T>(string json)
        {
            // not used in this test
            return default!;
        }

        public object? Deserialize(string json, Type type)
        {
            return null;
        }

        public string Serialize(object obj)
        {
            throw new InvalidOperationException("serialize failed");
        }
    }

    [Fact]
    public void ToObjDictionary_GenericDictionary_Empty_ReturnsEmpty()
    {
        var utils = _fixture.GetService<ICommonUtils>();
        var gd = new Dictionary<int, object>();
        var d = utils.ToObjDictionary("g", gd);
        Assert.Empty(d);
    }

    [Fact]
    public void ToObjDictionary_NonGenericDictionary_Empty_ReturnsEmpty()
    {
        var utils = _fixture.GetService<ICommonUtils>();
        var ht = new Hashtable();
        var d = utils.ToObjDictionary("h", ht);
        Assert.Empty(d);
    }
}
