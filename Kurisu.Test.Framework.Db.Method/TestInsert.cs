using System;
using System.Linq;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Abstractions;
using Kurisu.DataAccessor.Functions.Default.Abstractions;
using Kurisu.Test.Framework.Db.Method.DI;
using Microsoft.EntityFrameworkCore;
using weather;
using Xunit;

namespace Kurisu.Test.Framework.Db.Method
{
    [Trait("db", "insert")]
    public class TestInsert
    {
        private readonly IAppDbService _dbService;

        public TestInsert(IAppDbService appDbService)
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

            await _dbService.InsertAsync(data);
            var count = await _dbService.Queryable<WeatherForecast>().CountAsync();

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

            await _dbService.InsertAsync(data);
            await _dbService.SaveChangesAsync();
            var count = await _dbService.Queryable<WeatherForecast>().CountAsync();

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

            await _dbService.InsertRangeAsync(weatherForecasts);
            {
                //entities = null
            }
            await _dbService.SaveChangesAsync();

            var count = await _dbService.Queryable<WeatherForecast>().CountAsync();

            Assert.Equal(200, count);
        }

        [Fact]
        public async Task Insert_return_identity()
        {
            await DbSeedHelper.InitializeAsync(_dbService);

            var keyObj = await _dbService.InsertReturnIdentityAsync(new WeatherForecast
            {
                Date = DateTime.Now,
                TemperatureC = 100,
                Summary = "test",
            });

            Assert.Equal(101, keyObj);

            var keyInt = await _dbService.InsertReturnIdentityAsync<int>(new WeatherForecast
            {
                Date = DateTime.Now,
                TemperatureC = 100,
                Summary = "test",
            });

            Assert.Equal(102, keyInt);
        }
    }
}