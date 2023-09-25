using System;
using Kurisu.DataAccess.Entity;

namespace Kurisu.Test.Framework.Db.Method.Entities;

public class WeatherForecast : BaseEntity<int>, ISoftDeleted
{
    public DateTime Date { get; set; }

    public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int) (TemperatureC / 0.5556);

    public string Summary { get; set; }

    public bool IsDeleted { get; set; }
}