using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class ForecastQueryHandlerTests
{
    [Test]
    public async Task Handle_ShouldReturnFiveForecasts_WithRequiredFields()
    {
        var handler = new ForecastQueryHandler();

        var forecasts = await handler.Handle(new ForecastQuery(), CancellationToken.None);

        forecasts.ShouldNotBeNull();
        forecasts.Length.ShouldBe(5);
        foreach (var forecast in forecasts)
        {
            forecast.Date.ShouldBeGreaterThan(DateTime.Now.Date);
            forecast.Summary.ShouldNotBeNullOrWhiteSpace();
            forecast.TemperatureC.ShouldBeInRange(-20, 54);
        }
    }

    [Test]
    public async Task Handle_ShouldBeResolvableFromTestHost()
    {
        var handler = TestHost.GetRequiredService<ForecastQueryHandler>();

        var forecasts = await handler.Handle(new ForecastQuery(), CancellationToken.None);

        forecasts.Length.ShouldBe(5);
    }
}
