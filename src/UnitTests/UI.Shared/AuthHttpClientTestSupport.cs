using System.Net;
using System.Net.Http;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

/// <summary>
/// Returns <see cref="HttpStatusCode.NoContent"/> for auth API calls in bUnit component tests.
/// </summary>
internal sealed class StubAuthHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
}

/// <summary>
/// Registers an <see cref="HttpClient"/> suitable for login/logout component tests.
/// </summary>
internal static class AuthHttpClientTestSupport
{
    internal static HttpClient CreateClient() =>
        new(new StubAuthHttpMessageHandler())
        {
            BaseAddress = new Uri("http://localhost/")
        };
}
