using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record ForecastQuery : IRequest<WeatherForecast[]>, IRemotableRequest
{
}