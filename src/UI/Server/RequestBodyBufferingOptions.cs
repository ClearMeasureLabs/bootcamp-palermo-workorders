namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Configuration for request body buffering so the same HTTP request can re-read the body stream.
/// </summary>
public sealed class RequestBodyBufferingOptions
{
    public const string SectionName = "RequestBodyBuffering";

    /// <summary>
    /// When false, <see cref="RequestBodyBufferingExtensions.UseRequestBodyBuffering"/> does not enable body buffering.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum bytes buffered in memory before the framework may spill to disk (see ASP.NET Core buffering behavior).
    /// Default 1 MiB.
    /// </summary>
    public long BufferThreshold { get; set; } = 1024 * 1024;
}
