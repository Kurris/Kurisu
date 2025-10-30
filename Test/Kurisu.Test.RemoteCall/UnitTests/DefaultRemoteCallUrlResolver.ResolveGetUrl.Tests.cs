using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Kurisu.RemoteCall.Default;
using Kurisu.RemoteCall;
using Kurisu.RemoteCall.Utils;
using Kurisu.RemoteCall.Attributes;
using Xunit;

namespace Kurisu.Test.RemoteCall.UnitTests
{
    public class DefaultRemoteCallUrlResolver_ResolveGetUrl_Tests
    {
        private class FakeCommonUtils : ICommonUtils
        {
            public bool IsReferenceType(Type type) => TypeHelper.IsReferenceType(type);

            public Dictionary<string, object> ToObjDictionary(string prefix, object obj)
            {
                // Simple deterministic conversion for tests
                var dict = new Dictionary<string, object>();
                if (obj == null) return dict;

                // If object is a dictionary already, copy
                if (obj is IDictionary<string, object> od)
                {
                    foreach (var kv in od)
                    {
                        var key = string.IsNullOrEmpty(prefix) ? kv.Key : prefix + "." + kv.Key;
                        dict[key] = kv.Value!;
                    }
                    return dict;
                }

                // Use reflection to get public properties
                var t = obj.GetType();
                foreach (var pi in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var key = string.IsNullOrEmpty(prefix) ? pi.Name : prefix + "." + pi.Name;
                    dict[key] = pi.GetValue(obj)!;
                }

                return dict;
            }
        }

        private static void DummyMethodForParams(int id, string name) { }

        private static void DummyMethodComplex([RequestQuery("p")] object p) { }

        private static void DummyMethodObj([RequestQuery("pref")] MyDto dto) { }

        private class MyDto
        {
            public int A { get; set; }
            public string B { get; set; }
        }

        [Fact]
        public void ResolveGetUrl_WithSimpleParameters_AppendsQueryString()
        {
            // Arrange
            var asm = typeof(BaseRemoteCallUrlResolver).Assembly;
            var t = asm.GetType("Kurisu.RemoteCall.Default.DefaultRemoteCallUrlResolver", throwOnError: true);
            var ctor = t.GetConstructor(new Type[] { typeof(IConfiguration), typeof(ICommonUtils) });
            Assert.NotNull(ctor);

            var cfg = new ConfigurationBuilder().Build();
            var resolver = (DefaultRemoteCallUrlResolver)ctor.Invoke(new object[] { cfg, new FakeCommonUtils() });

            var mi = t.GetMethod("ResolveGetUrl", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(mi);

            var pInfos = typeof(DefaultRemoteCallUrlResolver_ResolveGetUrl_Tests).GetMethod(nameof(DummyMethodForParams), BindingFlags.NonPublic | BindingFlags.Static)!.GetParameters();
            var list = new List<ParameterValue>
            {
                new ParameterValue(pInfos[0], 42),
                new ParameterValue(pInfos[1], "hello world")
            };

            var template = "api/items";

            // Act
            var result = (string)mi.Invoke(resolver, new object[] { template, list })!;

            // Assert
            Assert.StartsWith("api/items?", result);
            Assert.Contains("id=42", result);
            // space should be url-encoded as + by WebUtility.UrlEncode
            Assert.Contains("name=hello+world", result);
        }

        [Fact]
        public void ResolveGetUrl_WhenParameterIsComplexObject_UsesToObjDictionary()
        {
            var asm = typeof(BaseRemoteCallUrlResolver).Assembly;
            var t = asm.GetType("Kurisu.RemoteCall.Default.DefaultRemoteCallUrlResolver", throwOnError: true);
            var ctor = t.GetConstructor(new Type[] { typeof(IConfiguration), typeof(ICommonUtils) });
            var cfg = new ConfigurationBuilder().Build();
            var resolver = (DefaultRemoteCallUrlResolver)ctor.Invoke(new object[] { cfg, new FakeCommonUtils() });

            var mi = t.GetMethod("ResolveGetUrl", BindingFlags.NonPublic | BindingFlags.Instance);

            var pInfo = typeof(DefaultRemoteCallUrlResolver_ResolveGetUrl_Tests).GetMethod(nameof(DummyMethodObj), BindingFlags.NonPublic | BindingFlags.Static)!.GetParameters()[0];

            var dto = new MyDto { A = 7, B = "x y" };
            var list = new List<ParameterValue> { new ParameterValue(pInfo, dto) };

            var template = "api/values";
            var result = (string)mi.Invoke(resolver, new object[] { template, list })!;

            // Expect keys pref.A and pref.B with proper encoding
            Assert.StartsWith("api/values?", result);
            Assert.Contains("pref.A=7", result);
            Assert.Contains("pref.B=x+y", result);
        }

        [Fact]
        public void ResolveGetUrl_WhenQueryAttributeOnObjectTypedAsObject_AndValueIsEnumerable_ExpandsElements()
        {
            var asm = typeof(BaseRemoteCallUrlResolver).Assembly;
            var t = asm.GetType("Kurisu.RemoteCall.Default.DefaultRemoteCallUrlResolver", throwOnError: true);
            var ctor = t.GetConstructor(new Type[] { typeof(IConfiguration), typeof(ICommonUtils) });
            var cfg = new ConfigurationBuilder().Build();
            var resolver = (DefaultRemoteCallUrlResolver)ctor.Invoke(new object[] { cfg, new FakeCommonUtils() });

            var mi = t.GetMethod("ResolveGetUrl", BindingFlags.NonPublic | BindingFlags.Instance);

            var pInfo = typeof(DefaultRemoteCallUrlResolver_ResolveGetUrl_Tests).GetMethod(nameof(DummyMethodComplex), BindingFlags.NonPublic | BindingFlags.Static)!.GetParameters()[0];

            var value = new List<int> { 1, 2, 3 };
            var list = new List<ParameterValue> { new ParameterValue(pInfo, value) };

            var template = "api/list";
            var result = (string)mi.Invoke(resolver, new object[] { template, list })!;

            Assert.StartsWith("api/list?", result);
            // should contain p=1&p=2&p=3 (order preserved)
            Assert.Contains("p=1", result);
            Assert.Contains("p=2", result);
            Assert.Contains("p=3", result);
        }

        [Fact]
        public void ResolveGetUrl_WhenQueryAttributeOnEnumerableParameter_ListOfInt_ExpandsElements()
        {
            var asm = typeof(BaseRemoteCallUrlResolver).Assembly;
            var t = asm.GetType("Kurisu.RemoteCall.Default.DefaultRemoteCallUrlResolver", throwOnError: true);
            var ctor = t.GetConstructor(new Type[] { typeof(IConfiguration), typeof(ICommonUtils) });
            var cfg = new ConfigurationBuilder().Build();
            var resolver = (DefaultRemoteCallUrlResolver)ctor.Invoke(new object[] { cfg, new FakeCommonUtils() });

            var mi = t.GetMethod("ResolveGetUrl", BindingFlags.NonPublic | BindingFlags.Instance);

            // create a ParameterValue that simulates a parameter annotated with [RequestQuery("ids")] and is List<int>
            var pInfo = typeof(DefaultRemoteCallUrlResolver_ResolveGetUrl_Tests).GetMethod(nameof(DummyMethodComplex), BindingFlags.NonPublic | BindingFlags.Static)!.GetParameters()[0];
            var value = new List<int> { 10, 20 };
            var list = new List<ParameterValue> { new ParameterValue(pInfo, value) };

            var template = "api/byids";
            var result = (string)mi.Invoke(resolver, new object[] { template, list })!;

            Assert.StartsWith("api/byids?", result);
            Assert.Contains("p=10", result);
            Assert.Contains("p=20", result);
        }
    }
}
