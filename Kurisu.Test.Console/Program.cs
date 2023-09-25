using System.Diagnostics;
using Kurisu.Utils;
using Kurisu.Utils.Extensions;

namespace Kurisu.Test.Console;

class Program
{
    static void Main(string[] args)
    {
        SnowFlakeHelper.Instance.NextId();

        var list = new List<long>(50000);
        var sw = Stopwatch.StartNew();
        for (var j = 0; j < 10000; j++)
        {
            list.Add(SnowFlakeHelper.Instance.NextId());
        }

        System.Console.WriteLine($"雪花id耗时:{sw.Elapsed}");
        System.Console.WriteLine($"雪花id个数:{list.Distinct().Count()}");

        System.Console.WriteLine("/*--------------------------------------------------------------------------------------------------*/");
        var testObject = new TestObject
        {
            Id = SnowFlakeHelper.Instance.NextId(),
            Name = "test"
        };

        var loopTimes = 50000000;

        sw.Restart();
        for (var i = 0; i < loopTimes; i++)
        {
            _ = testObject.Id;
            _ = testObject.Name;
        }


        System.Console.WriteLine($"原生方法耗时:{sw.Elapsed}");


        var idProperty = typeof(TestObject).GetProperty("Id");
        var id = testObject.GetExpressionPropertyValueWithGetMethod(idProperty.Name, out var getIdMethod);
        testObject.SetExpressionPropertyValueWithGetMethod(idProperty.Name, 1L, out var setIdMethod);
        sw.Restart();
        for (var i = 0; i < loopTimes; i++)
        {
            setIdMethod(testObject, 2L);
        }

        System.Console.WriteLine($"表达式树方法耗时:{sw.Elapsed}");

        sw.Restart();
        for (var i = 0; i < loopTimes; i++)
        {
            idProperty.SetValue(testObject, 3L);
        }

        System.Console.WriteLine($"反射方法耗时:{sw.Elapsed}");
    }
}