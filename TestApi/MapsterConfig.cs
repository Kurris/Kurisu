using Mapster;
using Microsoft.Extensions.Logging;

namespace TestApi
{
    public class MapsterConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<WeatherForecast, WeatherForecastDto>()
                .Map(dest => dest.Summary, src => src.Summary + "AAAAAAAA")
                .AfterMapping(dest =>
                {
                    var logger = MapContext.Current.GetService<ILogger<MapsterConfig>>();
                });
        }
    }
}