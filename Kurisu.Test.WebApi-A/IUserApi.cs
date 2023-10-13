using Kurisu.RemoteCall;
using Kurisu.UnifyResultAndValidation;
using Microsoft.AspNetCore.Mvc;

namespace Kurisu.Test.WebApi_A;


[EnableRemoteClient]
public interface IUserApi
{
    [HttpGet("http://localhost:5001/api/weatherforecast/page")]
    Task<DefaultApiResult<List<Entity.Test>>> GetHtmlAsync(string name, int pageIndex, int pageSize);
}
