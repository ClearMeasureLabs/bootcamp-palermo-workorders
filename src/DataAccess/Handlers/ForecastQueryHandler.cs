using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using MediatR;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class ForecastQueryHandler : IRequestHandler<ForecastQuery, WeatherForecast[]>
{
    public Task<WeatherForecast[]> Handle(ForecastQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new WeatherForecastData().GetAll());
    }

    private class WeatherForecastData
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };


        private readonly WeatherForecast[] AllForecasts;

        public WeatherForecastData()
        {
            var forecasts = new List<WeatherForecast>();
            for (var i = 1; i <= 5; i++)
            {
                var forecast = new WeatherForecast
                {
                    Date = DateTime.Now.AddDays(i), Id = i,
                    Summary = Summaries[Random.Shared.Next(Summaries.Length)],
                    TemperatureC = Random.Shared.Next(-20, 55)
                };
                forecasts.Add(forecast);
            }

            AllForecasts = forecasts.ToArray();
        }

        public WeatherForecast[] GetAll()
        {
            return AllForecasts;
        }
    }
}