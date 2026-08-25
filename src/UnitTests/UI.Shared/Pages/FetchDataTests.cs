using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class FetchDataTests
{
    [Test]
    public async Task ShouldLoadForecasts_FromBus_OnInitialize()
    {
        await using var ctx = new BunitContext();

        var forecasts = new[]
        {
            new WeatherForecast
            {
                Date = new DateTime(2026, 8, 21),
                TemperatureC = 28,
                Summary = "Sunny"
            },
            new WeatherForecast
            {
                Date = new DateTime(2026, 8, 22),
                TemperatureC = 18,
                Summary = "Rainy"
            }
        };

        ctx.Services.AddSingleton<IBus>(new StubForecastBus(forecasts));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        ctx.Services.AddLogging();

        var component = ctx.Render<FetchData>();

        await component.WaitForAssertionAsync(() =>
        {
            component.Instance.Model.ShouldNotBeNull();
            component.Instance.Model!.Length.ShouldBe(2);
            component.FindAll("tbody tr.weather-row").Count.ShouldBe(2);
            component.Markup.ShouldContain("Sunny");
            component.Markup.ShouldContain("Rainy");
            component.Markup.ShouldContain("Great for outdoor services");
            component.Markup.ShouldContain("Indoor activities recommended");
        });
    }

    private sealed class StubForecastBus(WeatherForecast[] forecasts) : IBus
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is ForecastQuery)
            {
                return Task.FromResult((TResponse)(object)forecasts);
            }

            throw new NotSupportedException(request.GetType().Name);
        }

        public Task<object?> Send(object request) => throw new NotSupportedException();

        public Task Publish(INotification notification) => throw new NotSupportedException();
    }
}
