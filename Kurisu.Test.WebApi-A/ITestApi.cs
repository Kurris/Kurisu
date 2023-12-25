using Kurisu.AspNetCore.UnifyResultAndValidation;
using Kurisu.RemoteCall.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Kurisu.Test.WebApi_A;

[EnableRemoteClient(Name = "user-service", BaseUrl = "http://localhost:5001")]
[Log]
public interface ITestApi
{
    // [HttpGet("${XXXApi:A}")]
    // Task<DefaultApiResult<List<Entity.Test>>> GetHtmlAsync([FromQuery] NameInput input, int pageIndex, int pageSize);
    //
    // [HttpGet("http://localhost/user/{name}")]
    // Task<DefaultApiResult<List<Entity.Test>>> GetHtmlAsync(string name, int pageIndex, int pageSize);


    [Get("api/weatherforecast/string")]
    Task<DefaultApiResult<string>> GetString();

    [Get("https://zhuanlan.zhihu.com/p/455022361")]
    string GetB();


    [Put("/api/weatherforecast/{id:int}")]
    Task PutAsync(int id);


    [Post("/api/weatherforecast/upload")]
    Task UploadAsync([FromForm] byte[] bytes, string file, string fileName);


    // [HttpPost("/api/weatherforecast/post")]
    // Task<DefaultApiResult<object>> SendMsg(NameInput nameInput);
}