using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Writes a detailed JSON health check response for the <c>/_healthcheck/detailed</c> endpoint,
/// including exception messages, stack traces, descriptions, durations, and data dictionaries
/// for each registered health check component.
/// </summary>
public static class DetailedHealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    /// <summary>
    /// Writes the <paramref name="report"/> as a detailed JSON payload to the HTTP response.
    /// </summary>
    public static async Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new DetailedHealthCheckResponse
        {
            OverallStatus = report.Status.ToString(),
            TotalDurationMs = report.TotalDuration.TotalMilliseconds,
            Entries = report.Entries
                .OrderBy(e => e.Key, StringComparer.Ordinal)
                .Select(pair => new DetailedHealthCheckEntry
                {
                    Name = pair.Key,
                    Status = pair.Value.Status.ToString(),
                    Description = pair.Value.Description,
                    DurationMs = pair.Value.Duration.TotalMilliseconds,
                    ExceptionMessage = pair.Value.Exception?.Message,
                    ExceptionDetail = pair.Value.Exception?.ToString(),
                    Data = pair.Value.Data.Count > 0
                        ? pair.Value.Data.ToDictionary(
                            d => d.Key,
                            d => d.Value?.ToString() ?? string.Empty)
                        : null,
                    Tags = pair.Value.Tags.Any() ? pair.Value.Tags.ToList() : null
                })
                .ToList()
        };

        await context.Response.WriteAsJsonAsync(response, JsonOptions);
    }

    internal sealed class DetailedHealthCheckResponse
    {
        /// <summary>Overall aggregated status of all health checks.</summary>
        public required string OverallStatus { get; init; }

        /// <summary>Total duration in milliseconds for all health checks combined.</summary>
        public required double TotalDurationMs { get; init; }

        /// <summary>Per-component health check results with full diagnostic context.</summary>
        public required IReadOnlyList<DetailedHealthCheckEntry> Entries { get; init; }
    }

    internal sealed class DetailedHealthCheckEntry
    {
        /// <summary>Registered name of the health check.</summary>
        public required string Name { get; init; }

        /// <summary>Health status result (Healthy, Degraded, Unhealthy).</summary>
        public required string Status { get; init; }

        /// <summary>Human-readable description returned by the health check.</summary>
        public string? Description { get; init; }

        /// <summary>Duration in milliseconds the health check took to execute.</summary>
        public double DurationMs { get; init; }

        /// <summary>Exception message when the check captured a failure.</summary>
        public string? ExceptionMessage { get; init; }

        /// <summary>Full exception detail including stack trace for production issue diagnosis.</summary>
        public string? ExceptionDetail { get; init; }

        /// <summary>Arbitrary key-value data reported by the health check.</summary>
        public IReadOnlyDictionary<string, string>? Data { get; init; }

        /// <summary>Tags associated with this health check registration.</summary>
        public IReadOnlyList<string>? Tags { get; init; }
    }
}
