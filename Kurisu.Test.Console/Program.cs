using System.Security.Claims;
using IdentityModel;
using Kurisu.Test.Console;
using System.Diagnostics;
using Kurisu.AspNetCore.Authentication;
using Kurisu.AspNetCore.Utils;
using Kurisu.AspNetCore.Utils.Extensions;

Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;


var sw = Stopwatch.StartNew();
var token = JwtEncryption.GenerateToken(new Claim[]
{
            new(JwtClaimTypes.Subject, "1"),
            new(JwtClaimTypes.Name, "小李"),
},
 new Claim[]
 {
             new Claim("role-detail","menu:add")
 }
, "123456789XCVBN@123", "ligy", "api");


var uid = SnowFlakeHelper.Instance.NextId();
var dt = SnowFlakeHelper.AnalyzeId(uid);


var list = new List<long>(5000000);
sw.Restart();
for (var j = 0; j < 5000000; j++)
{
    list.Add(SnowFlakeHelper.Instance.NextId());
}

Console.WriteLine($"雪花id耗时:{sw.Elapsed}");
Console.WriteLine($"雪花id个数:{list.Select(x => x.ToString().Length).Distinct().Count()}");

Console.WriteLine("/*--------------------------------------------------------------------------------------------------*/");
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


Console.WriteLine($"原生方法耗时:{sw.Elapsed}");

var a = testObject.GetPropertyValue(x => x.Name);
a = testObject.GetPropertyValue(x => x.Name);
var idProperty = typeof(TestObject).GetProperty("Id");
sw.Restart();
for (var i = 0; i < loopTimes; i++)
{
    //setIdMethod(testObject, 2L);
}

Console.WriteLine($"表达式树方法耗时:{sw.Elapsed}");

sw.Restart();
for (var i = 0; i < loopTimes; i++)
{
    idProperty.SetValue(testObject, 3L);
}

Console.WriteLine($"反射方法耗时:{sw.Elapsed}");

