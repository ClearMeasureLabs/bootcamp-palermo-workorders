// ReSharper disable PropertyCanBeMadeInitOnly.Global -- Qodana P5 (#9440): NHibernate proxy / System.Text.Json set-by-convention requires mutable setters

namespace ClearMeasure.Bootcamp.Core;

public class ConfigurationModel
{
    public string? AppInsightsConnectionString { get; set; }
}