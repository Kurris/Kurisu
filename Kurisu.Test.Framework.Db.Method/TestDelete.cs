using System.Collections.Generic;
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
    [Trait("db", "delete")]
    public class TestDelete
    {
        private readonly IAppDbService _dbService;

        public TestDelete(IAppDbService dbService)
        {
            _dbService = dbService;
        }

        [Fact]
        public async Task DeleteFirstData_without_saveChanges()
        {
            await DbSeedHelper.InitializeAsync(_dbService);

            var weatherForecasts = await _dbService.FirstOrDefaultAsync<WeatherForecast>();

            await _dbService.DeleteAsync(weatherForecasts);

            var list = await GetWeatherForecastList();

            Assert.Equal(100, list.Count);
        }

        [Fact]
        public async Task DeleteFirstData_with_saveChanges()
        {
            await DbSeedHelper.InitializeAsync(_dbService);

            var weatherForecasts = await _dbService.FirstOrDefaultAsync<WeatherForecast>();

            await _dbService.DeleteAsync(weatherForecasts);

            var effectRows = await _dbService.SaveChangesAsync();
            Assert.Equal(1, effectRows);

            var list = await GetWeatherForecastList();

            Assert.Equal(99, list.Count);
        }


        private async Task<List<WeatherForecast>> GetWeatherForecastList()
        {
            var res = await _dbService.Queryable<WeatherForecast>().OrderBy(x => x.Date).ToListAsync();
            return res;
        }
    }
}