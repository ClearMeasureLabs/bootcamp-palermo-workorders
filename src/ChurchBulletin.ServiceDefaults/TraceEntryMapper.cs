using System.Diagnostics;

namespace ChurchBulletin.ServiceDefaults;

internal static class TraceEntryMapper
{
    public static TraceEntry FromActivity(Activity activity, string status)
    {
        return new TraceEntry
        {
            Timestamp = DateTime.UtcNow,
            Status = status,
            TraceId = activity.TraceId.ToString(),
            SpanId = activity.SpanId.ToString(),
            ParentSpanId = activity.ParentSpanId != default ? activity.ParentSpanId.ToString() : null,
            Name = activity.DisplayName,
            Source = activity.Source.Name,
            DurationMs = activity.Duration.TotalMilliseconds,
            StatusCode = activity.Status != ActivityStatusCode.Unset ? activity.Status.ToString() : null,
            Error = activity.Status == ActivityStatusCode.Error ? activity.StatusDescription : null,
            Tags = activity.Tags.ToDictionary(t => t.Key, t => t.Value)
        };
    }
}
