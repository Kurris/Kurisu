using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Dto;
using Kurisu.DataAccessor.Functions.Default.Abstractions;
using Kurisu.Test.Framework.Db.Method.DI;
using Kurisu.Test.Framework.Db.Method.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kurisu.Test.Framework.Db.Method
{
    [Trait("db", "query")]
    public class TestQuery
    {
        private readonly IAppDbService _dbService;

        public TestQuery(IAppDbService dbService)
        {
            _dbService = dbService;
        }


        [Fact]
        public async Task QueryAll_return_100()
        {
            await DbSeedHelper.InitializeAsync(_dbService);
            var weatherForecasts = await GetWeatherForecastList();

            var count = weatherForecasts.Count;

            Assert.Equal(100, count);
        }


        [Fact]
        public async Task QueryFirstOrDefault_return_object()
        {
            await DbSeedHelper.InitializeAsync(_dbService);
            var weatherForecasts = await _dbService.FirstOrDefaultAsync<WeatherForecast>();

            var exists = weatherForecasts != null;

            Assert.True(exists);
        }

        [Theory]
        [InlineData(1, 10)]
        [InlineData(2, 10)]
        [InlineData(10, 30)]
        [InlineData(4, 10)]
        [InlineData(5, 30)]
        public async Task QueryPage_with_right_pageInput(int pageIndex, int pageSize)
        {
            await DbSeedHelper.InitializeAsync(_dbService);

            var pages = await _dbService.Queryable<WeatherForecast>().ToPageAsync(pageIndex, pageSize);
            var total = await _dbService.Queryable<WeatherForecast>().CountAsync();

            Assert.Equal(total, pages.Total);

            var ids = pages.Data.Select(x => x.Id);

            var dbIds = Enumerable.Range(1, 100);
            var idPage = dbIds.ToPage(pageIndex, pageSize);

            Assert.All(idPage.Data, x => Assert.Contains(x, ids));
            Assert.All(ids, x => Assert.Contains(x, idPage.Data));
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(2, 0)]
        [InlineData(-1, 30)]
        [InlineData(4, -1)]
        [InlineData(-5, -1)]
        public async Task QueryPage_with_wrong_pageInput(int pageIndex, int pageSize)
        {
            await DbSeedHelper.InitializeAsync(_dbService);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => { await _dbService.Queryable<WeatherForecast>().ToPageAsync(pageIndex, pageSize); });

            var dbIds = Enumerable.Range(1, 100);
            Assert.Throws<ArgumentOutOfRangeException>(() => dbIds.ToPage(pageIndex, pageSize));
        }


        [Fact]
        public async Task Query_weather_list_fromSql()
        {
            await DbSeedHelper.InitializeAsync(_dbService);

            var sql = "select * from WeatherForecast";
            var results = await _dbService.ToListAsync<WeatherForecast>(sql);

            Assert.Equal(100, results.Count);
        }

        [Fact]
        public async Task Query_weather_single_fromSql()
        {
            await DbSeedHelper.InitializeAsync(_dbService);

            var sql = "select top 1 from WeatherForecast ";
            var result = await _dbService.FirstOrDefaultAsync<WeatherForecast>(sql);

            Assert.NotNull(result);

            sql = "select top 1 from WeatherForecast limit 1";
            result = await _dbService.FirstOrDefaultAsync<WeatherForecast>(sql);

            Assert.NotNull(result);
        }


        private async Task<List<WeatherForecast>> GetWeatherForecastList()
        {
            var res = await _dbService.Queryable<WeatherForecast>().OrderBy(x => x.Date).ToListAsync();
            return res;
        }
    }
}