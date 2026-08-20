namespace ClearMeasure.Bootcamp.AcceptanceTests;

/// <summary>
/// Shared <see cref="HttpClient"/> for acceptance-test HTTP calls against the local/dev
/// application (self-signed certificate). Resolves Qodana <c>ShortLivedHttpClient</c>
/// without introducing a DI container or new NuGet packages.
/// </summary>
/// <remarks>
/// Callers must pass absolute URIs and must not set <see cref="HttpClient.BaseAddress"/> —
/// mutating BaseAddress after any request throws <see cref="InvalidOperationException"/>.
/// </remarks>
internal static class TestHttpClientFactory
{
    private static readonly HttpClientHandler SharedHandler = new()
    {
        ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };

    private static readonly HttpClient SharedClient = new(SharedHandler)
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    /// <summary>
    /// Returns the process-lifetime shared client. Do not dispose the returned instance
    /// (ownership stays with this type); create-per-call disposal patterns are unnecessary
    /// because the underlying handler is shared.
    /// </summary>
    public static HttpClient CreateInsecureClient() => SharedClient;
}
