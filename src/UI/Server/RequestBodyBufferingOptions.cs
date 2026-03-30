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
    /// In-memory threshold (bytes) passed to <c>EnableBuffering(bufferThreshold, bufferLimit)</c> before the framework may spill to disk.
    /// Values above <see cref="int.MaxValue"/> are clamped. Values below 1 are treated as 1. Body read limit uses <c>long.MaxValue</c>.
    /// </summary>
    public long BufferThreshold { get; set; } = 1024 * 1024;
}
