using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Kurisu.DataAccess.Functions.Default.Abstractions;
using Kurisu.Test.Framework.Db.Method.Entities;
using Kurisu.Utils.Extensions;

namespace Kurisu.Test.Framework.Db.Method.DI;

public class DbSeedHelper
{
    public static readonly string[] Summaries =
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    public static async Task InitializeAsync(IDbWrite dbService)
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
        var dbContext = dbService.GetDbContext() as TestAppDbContext;
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
        dbContext.WeatherForecasts.AddRange(weatherForecasts);
        await dbContext.SaveChangesAsync();
    }
}