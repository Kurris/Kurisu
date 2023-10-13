using System.Security.Claims;
using IdentityModel;
using Kurisu.Authentication;
using Kurisu.Utils;
using Kurisu.Utils.Extensions;
using Kurisu.Test.Console;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Rewrite;

Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

//json
var persons = new List<Person>() { new Person() { Id = 1, Name = "小李" }, new Person() { Id = 2, Name = "小王" } };

var sap = new List<Person>() { new Person() { Id = 1, Name = "小赵" }, new Person() { Id = 2, Name = "小王" }, new Person() { Id = 3, Name = "小X" } };

//persons = persons.Except(sap, new PersonEqualityComparer()).ToList();

var  spersons = sap.Intersect(persons, new PersonEqualityComparer());
for (int i = 0; i < spersons.Count(); i++)
{
    sap.Remove(spersons.ElementAt(i));
}


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


SnowFlakeHelper.Instance.NextId();

var list = new List<long>(50000);
sw.Restart();
for (var j = 0; j < 10000; j++)
{
    list.Add(SnowFlakeHelper.Instance.NextId());
}

Console.WriteLine($"雪花id耗时:{sw.Elapsed}");
Console.WriteLine($"雪花id个数:{list.Distinct().Count()}");

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


class Person
{
    public int Id { get; set; }
    public string Name { get; set; }
}


class PersonEqualityComparer : IEqualityComparer<Person>
{
    public bool Equals(Person x, Person y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;
        if (x.GetType() != y.GetType()) return false;
        return x.Id == y.Id && x.Name == x.Name;
    }

    public int GetHashCode(Person obj)
    {
        if (Object.ReferenceEquals(obj, null)) return 0;
        //return HashCode.Combine(obj.Id, obj.Name);

        return obj.Id.GetHashCode() ^ obj.Name.GetHashCode();
    }

    //public bool Equals(Person x, Person y)
    //{
    //    //Check whether the compared objects reference the same data.
    //    if (Object.ReferenceEquals(x, y)) return true;

    //    //Check whether any of the compared objects is null.
    //    if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
    //        return false;

    //    //Check whether the products' properties are equal.
    //    return x.Equals(y);
    //}

    //public int GetHashCode(Person obj)
    //{
    //    //Check whether the object is null
    //    if (Object.ReferenceEquals(obj, null)) return 0;
    //    return obj.GetHashCode();
    //}


}