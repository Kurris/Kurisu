using Kurisu.AspNetCore.DynamicApi.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Kurisu.Test.WebApi_A.Dynamic;

[DynamicApi("api/apple")]
public class AppleAppService
{
    [HttpGet("something")]
    public string HttpGetA(string a)
    {
        return a;
    }
}