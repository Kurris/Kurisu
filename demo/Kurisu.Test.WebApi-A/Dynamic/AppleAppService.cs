using Kurisu.AspNetCore.DynamicApi.Attributes;
using Kurisu.Test.WebApi_A.Aops;
using Microsoft.AspNetCore.Mvc;

namespace Kurisu.Test.WebApi_A.Dynamic;

[AsApi("api/apple")]
public class AppleAppService
{
    [TestAop]
    [HttpGet("something")]
    public virtual string HttpGetA(string a)
    {
        return a;
    }
}