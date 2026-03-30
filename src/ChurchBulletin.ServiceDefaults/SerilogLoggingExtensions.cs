using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Configures Serilog as the primary logging pipeline with newline-delimited JSON console output.
/// </summary>
public static class SerilogLoggingExtensions
{
    /// <summary>
    /// Clears default logging providers and registers Serilog, reading configuration from the <c>Serilog</c> section when present.
    /// </summary>
    public static TBuilder AddSerilogWithJsonConsole<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.ClearProviders();
        builder.Services.AddSerilog((_, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", builder.Environment.ApplicationName);
        });

        return builder;
    }
}
