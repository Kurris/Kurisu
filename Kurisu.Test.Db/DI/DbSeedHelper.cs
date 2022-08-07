using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Abstractions;
using Kurisu.DataAccessor.Abstractions.Operation;
using Kurisu.DataAccessor.ReadWriteSplit.Abstractions;
using Kurisu.Utils.Extensions;
using weather;

namespace Kurisu.Test.Db.DI
{
    public class DbSeedHelper
    {
        public static readonly string[] Summaries =
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public static async Task InitializeAsync(IAppMasterDb dbService)
        {
            // var rng = new Random();
            // var weatherForecasts = Enumerable.Range(1, 100).Select(index => new WeatherForecast
            // {
            //     Date = DateTime.Now.AddDays(index),
            //     TemperatureC = rng.Next(-20, 55),
            //     Summary = Summaries[rng.Next(Summaries.Length)]
            // });

            var json = await File.ReadAllTextAsync("./WeatherForecast.json");
            var weatherForecasts = json.ToObject<List<WeatherForecast>>();
            var dbContext = dbService.GetMasterDbContext() as TestAppDbContext;
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();
            dbContext.WeatherForecasts.AddRange(weatherForecasts);
            await dbContext.SaveChangesAsync();
        }
    }
}