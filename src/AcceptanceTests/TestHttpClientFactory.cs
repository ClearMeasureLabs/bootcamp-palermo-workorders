using Microsoft.Extensions.DependencyInjection;

namespace ClearMeasure.Bootcamp.AcceptanceTests;

/// <summary>
/// Provides a shared <see cref="IHttpClientFactory"/> for acceptance-test HTTP calls that
/// talk to the locally hosted (or container-hosted) application over HTTPS with a
/// self-signed / dev certificate.
/// </summary>
/// <remarks>
/// Resolves Qodana <c>ShortLivedHttpClient</c> findings: test code previously created a new
/// <see cref="HttpClient"/> (and a new <see cref="HttpClientHandler"/>) on every call, which
/// exhausts sockets under repeated invocation (retry loops, per-request polling, etc.).
/// Clients handed out here pool the underlying <see cref="HttpMessageHandler"/> via
/// <see cref="IHttpClientFactory"/> instead.
/// <para>
/// Clients from this factory are never given a <see cref="HttpClient.BaseAddress"/> —
/// callers always pass a fully absolute URI to <c>GetAsync</c>/<c>PostAsync</c>. This is
/// deliberate: mutating <see cref="HttpClient.BaseAddress"/> after a client has issued a
/// request throws <see cref="InvalidOperationException"/>, and a pooled client's
/// underlying handler can outlive any single caller, so no caller may assume it owns the
/// client exclusively.
/// </para>
/// </remarks>
internal static class TestHttpClientFactory
{
    private static readonly Lazy<IHttpClientFactory> LazyFactory = new(BuildFactory);

    /// <summary>
    /// Creates a client configured to accept the local/dev self-signed certificate. Safe to
    /// call repeatedly and dispose after each use — the underlying handler is pooled by the
    /// factory, not recreated per call.
    /// </summary>
    public static HttpClient CreateInsecureClient() => LazyFactory.Value.CreateClient("insecure");

    private static IHttpClientFactory BuildFactory()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("insecure", client => client.Timeout = TimeSpan.FromSeconds(30))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });
        return services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
    }
}
