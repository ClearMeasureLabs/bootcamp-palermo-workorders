namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>
///     Template class from sample app - moved from UI.Shared
/// </summary>
public record WeatherForecast
{
    public DateTime Date { get; set; }

    public int TemperatureC { get; set; }

    public string? Summary { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public int Id { get; set; }
}