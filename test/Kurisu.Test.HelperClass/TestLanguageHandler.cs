using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kurisu.AspNetCore.MultiLanguage;
using Kurisu.AspNetCore.Utils.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Kurisu.Test.HelperClass
{
    public class TestLanguageHandler
    {
        [Fact]
        public void HandleResult_Null_ReturnsNull()
        {
            var result = LanguageHandler.HandleResult("en", null);
            Assert.Null(result);
        }

        [Fact]
        public void HandleResult_String_ReturnsSame()
        {
            var input = "hello";
            var result = LanguageHandler.HandleResult("en", input);
            Assert.Equal(input, result);
        }

        [Fact]
        public void HandleResult_Class_zh_KeepsBaseAndRemovesSuffixes()
        {
            var model = new TestModel { Title = "默认", Titleen = "EN", Description = "desc", Descriptionfr = "fr" };

            var result = LanguageHandler.HandleResult("zh", model);
            var json = JsonConvert.SerializeObject(result, JsonExtensions.DefaultSetting);
            var token = JToken.Parse(json);

            // base property should be original
            Assert.Equal("默认", token["title"]?.Value<string>());
            // suffixed properties removed
            Assert.Null(token["titleen"]);
            Assert.Equal("desc", token["description"]?.Value<string>());
            Assert.Null(token["descriptionfr"]);
        }

        [Fact]
        public void HandleResult_Class_en_UsesSuffixValueAndRemovesOthers()
        {
            var model = new TestModel { Title = "默认", Titleen = "EN", Description = "desc", Descriptionfr = "fr" };

            var result = LanguageHandler.HandleResult("en", model);
            var json = JsonConvert.SerializeObject(result, JsonExtensions.DefaultSetting);
            var token = JToken.Parse(json);

            // base property should be replaced by Titleen
            Assert.Equal("EN", token["title"]?.Value<string>());
            // suffixed properties removed
            Assert.Null(token["titleen"]);
            // description has a fr suffix but language is en -> base remains
            Assert.Equal("desc", token["description"]?.Value<string>());
            Assert.Null(token["descriptionfr"]);
        }

        [Fact]
        public void HandleResult_ListOfObjects_TransformsEachItem()
        {
            var list = new List<TestModel>
            {
                new TestModel { Title = "默认1", Titleen = "EN1" },
                new TestModel { Title = "默认2", Titleen = "EN2" }
            };

            var result = LanguageHandler.HandleResult("en", list);

            var json = JsonConvert.SerializeObject(result, JsonExtensions.DefaultSetting);
            var token = JToken.Parse(json);

            Assert.Equal("EN1", token[0]["title"]?.Value<string>());
            Assert.Equal("EN2", token[1]["title"]?.Value<string>());
            Assert.Null(token[0]["titleen"]);
            Assert.Null(token[1]["titleen"]);
        }

        [Fact]
        public void HandleResult_Dictionary_ReturnsOriginal()
        {
            var dict = new Dictionary<string, TestModel>
            {
                ["a"] = new TestModel { Title = "默认1", Titleen = "EN1" },
                ["b"] = new TestModel { Title = "默认2", Titleen = "EN2" }
            };

            var result = LanguageHandler.HandleResult("en", dict);

            Assert.NotNull(result);
            var json = JsonConvert.SerializeObject(result, JsonExtensions.DefaultSetting);
            var token = JToken.Parse(json);

            Assert.Equal("EN1", token["a"]["title"]?.Value<string>());
            Assert.Equal("EN2", token["b"]["title"]?.Value<string>());
        }

        private class TestModel
        {
            public string Title { get; set; }
            public string Titleen { get; set; }
            public string Description { get; set; }
            public string Descriptionfr { get; set; }
        }

        private class ParentModel
        {
            public string Name { get; set; }
            public TestModel Child { get; set; }
        }

        private class IgnoredModel
        {
            public string Keep { get; set; }
            [JsonIgnore]
            public string Skip { get; set; }
            public string SkipEn { get; set; }
        }

        [Fact]
        public void HandleResult_ArrayOfObjects_TransformsEachItem()
        {
            var arr = new TestModel[]
            {
                new TestModel { Title = "默认1", Titleen = "EN1" },
                new TestModel { Title = "默认2", Titleen = "EN2" }
            };

            var result = LanguageHandler.HandleResult("en", arr);

            var json = JsonConvert.SerializeObject(result, JsonExtensions.DefaultSetting);
            var token = JToken.Parse(json);

            Assert.Equal("EN1", token[0]["title"]?.Value<string>());
            Assert.Equal("EN2", token[1]["title"]?.Value<string>());
        }

        [Fact]
        public void HandleResult_NonGenericIList_TransformsItems()
        {
            var al = new ArrayList
            {
                new TestModel { Title = "默认1", Titleen = "EN1" },
                new TestModel { Title = "默认2", Titleen = "EN2" }
            };
            var result = LanguageHandler.HandleResult("en", al);

            var json = JsonConvert.SerializeObject(result, JsonExtensions.DefaultSetting);
            var token = JToken.Parse(json);

            Assert.Equal("EN1", token[0]["title"]?.Value<string>());
            Assert.Equal("EN2", token[1]["title"]?.Value<string>());
        }

        [Fact]
        public void HandleResult_PrimitiveList_ReturnsOriginal()
        {
            var list = new List<int> { 1, 2, 3 };
            var result = LanguageHandler.HandleResult("en", list);

            Assert.Same(list, result);
        }

        [Fact]
        public void HandleResult_StringList_ReturnsOriginal()
        {
            var list = new List<string> { "a", "b" };
            var result = LanguageHandler.HandleResult("en", list);

            Assert.Same(list, result);
        }

        [Fact]
        public void HandleResult_NonGenericDictionary_TransformsValues()
        {
            var ht = new Hashtable();
            ht["a"] = new TestModel { Title = "默认", Titleen = "EN" };
            ht[1] = new TestModel { Title = "默认2", Titleen = "EN2" };

            var result = LanguageHandler.HandleResult("en", ht);

            var json = JsonConvert.SerializeObject(result, JsonExtensions.DefaultSetting);
            var token = JToken.Parse(json);

            Assert.Equal("EN", token["a"]["title"]?.Value<string>());
            Assert.Equal("EN2", token["1"]["title"]?.Value<string>());
        }

        [Fact]
        public void HandleResult_NestedObject_TransformsChild()
        {
            var parent = new ParentModel { Name = "p", Child = new TestModel { Title = "默认", Titleen = "EN" } };
            var result = LanguageHandler.HandleResult("en", parent);

            var json = JsonConvert.SerializeObject(result, JsonExtensions.DefaultSetting);
            var token = JToken.Parse(json);

            Assert.Equal("EN", token["child"]["title"]?.Value<string>());
        }

        [Fact]
        public void HandleResult_EmptyList_ReturnsEmptyArray()
        {
            var list = new List<TestModel>();
            var result = LanguageHandler.HandleResult("en", list);

            var json = JsonConvert.SerializeObject(result, JsonExtensions.DefaultSetting);
            var token = JToken.Parse(json);
            Assert.True(token.Type == JTokenType.Array);
            Assert.Empty(token);
        }

        [Fact]
        public void HandleResult_JsonIgnore_PropertySkipped()
        {
            var model = new IgnoredModel { Keep = "k", Skip = "s", SkipEn = "SE" };
            var result = LanguageHandler.HandleResult("en", model);

            var json = JsonConvert.SerializeObject(result, JsonExtensions.DefaultSetting);
            var token = JToken.Parse(json);

            Assert.Equal("k", token["keep"]?.Value<string>());
            Assert.Null(token["skip"]);
            // skipen should be removed as a suffix variant
            Assert.Equal("SE", token["skipEn"]);
        }
    }
}
