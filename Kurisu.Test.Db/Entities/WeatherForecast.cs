using System;
using Kurisu.DataAccessor.Entity;

namespace weather
{
    public class WeatherForecast : BaseEntity<int>, ISoftDeleted
    {
        public DateTime Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int) (TemperatureC / 0.5556);

        public string Summary { get; set; }
        public DateTime? DeleteTimed { get; set; }
    }
}