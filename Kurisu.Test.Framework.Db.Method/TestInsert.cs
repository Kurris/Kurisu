using System;
using System.Linq;
using System.Threading.Tasks;
using Kurisu.DataAccess.Functions.Default.Abstractions;
using Kurisu.Test.Framework.Db.Method.DI;
using Kurisu.Test.Framework.Db.Method.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kurisu.Test.Framework.Db.Method;

[Trait("db", "insert")]
public class TestInsert
{
    private readonly IDbService _dbService;

    public TestInsert(IDbService appDbService)
    {
        _dbService = appDbService;
    }

    [Fact]
    public async Task Insert_one_return_100_without_saveChanges()
    {
        await DbSeedHelper.InitializeAsync(_dbService);

        var data = new WeatherForecast
        {
            Date = DateTime.Now,
            TemperatureC = 100,
            Summary = "test"
        };

        await _dbService.AddAsync(data);
        var count = await _dbService.AsQueryable<WeatherForecast>().CountAsync();

        Assert.Equal(100, count);
    }


    [Fact]
    public async Task Insert_one_return_100_with_saveChanges()
    {
        await DbSeedHelper.InitializeAsync(_dbService);
        var data = new WeatherForecast
        {
            Date = DateTime.Now,
            TemperatureC = 100,
            Summary = "test"
        };

        await _dbService.AddAsync(data);
        await _dbService.SaveChangesAsync();
        var count = await _dbService.AsQueryable<WeatherForecast>().CountAsync();

        Assert.Equal(101, count);
    }


    [Fact]
    public async Task BatchInsert_return_right_total()
    {
        await DbSeedHelper.InitializeAsync(_dbService);

        var rng = new Random();
        var weatherForecasts = Enumerable.Range(1, 100).Select(index => new WeatherForecast
        {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = rng.Next(-20, 55),
            Summary = DbSeedHelper.Summaries[rng.Next(DbSeedHelper.Summaries.Length)]
        });

        //weatherForecasts   0X2314
        //entities = 0X2314

        await _dbService.AddRangeAsync(weatherForecasts);
        {
            //entities = null
        }
        await _dbService.SaveChangesAsync();

        var count = await _dbService.AsQueryable<WeatherForecast>().CountAsync();

        Assert.Equal(200, count);
    }

    [Fact]
    public async Task Insert_return_identity()
    {
        await DbSeedHelper.InitializeAsync(_dbService);

        var o1 = new WeatherForecast
        {
            Date = DateTime.Now,
            TemperatureC = 100,
            Summary = "test",
        };

        await _dbService.AddAsync(o1);
        Assert.Equal(101, o1.Id);

        var o2 = new WeatherForecast
        {
            Date = DateTime.Now,
            TemperatureC = 100,
            Summary = "test",
        };

        await _dbService.AddAsync(o2);
        Assert.Equal(102, o2.Id);
    }
}