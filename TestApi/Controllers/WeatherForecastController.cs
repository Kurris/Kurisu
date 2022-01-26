// using System;
// using System.Threading.Tasks;
// using Kurisu.Authorization;
// using Kurisu.Cors;
// using Kurisu.DataAccessor.Abstractions;
// using Kurisu.DataAccessor.UnitOfWork.Attributes;
// using Kurisu.UnifyResultAndValidation;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.Extensions.Options;
// using TestApi.Entities;
// using TestApi.Services;
//
// namespace TestApi.Controllers
// {
//     [ApiController]
//     public class WeatherForecastController : ControllerBase
//     {
//         private readonly ITestService _service;
//         private readonly IOptions<CorsAppSetting> _options;
//         private readonly IOptions<JwtAppSetting> _jwt;
//         private readonly IMasterDbService _defaultDb;
//         private readonly ISlaveDbService _readDb;
//         private readonly AService _aService;
//
//         public WeatherForecastController(ITestService testService
//             , IOptions<CorsAppSetting> options
//             , IOptions<JwtAppSetting> jwt
//             , IMasterDbService db
//             , ISlaveDbService readDb
//             , AService aService)
//         {
//             _service = testService;
//             _options = options;
//             _jwt = jwt;
//             _defaultDb = db;
//             _readDb = readDb;
//             _aService = aService;
//         }
//
//
//         //     var id = Guid.NewGuid();
//         //     // //await _db.AddAsync();
//
//         // var users = await _readDb.FindListAsync<User>();
//         //
//         // await _defaultDb.AddAsync(new User()
//         // {
//         //     Id = Guid.NewGuid(),
//         //     Name = "shx",
//         //     Age = 23,
//         // });
//
//         //`
//         //     var datas = await _readDb.FindListAsync<User>();
//         //     //
//         //     // await _db.SaveAsync(new User()
//         //     // {
//         //     //     Age = 23,
//         //     //     Id = id,
//         //     //     Name = "ligy"
//         //     // });
//         //
//         //     await _db.UpdateAsync(new User()
//         //     {
//         //         Id = new Guid("853645c0-b6bf-4a75-a5f7-5cb4e1e09d6b"),
//         //         Age = 30,
//         //     });
//     }
// }