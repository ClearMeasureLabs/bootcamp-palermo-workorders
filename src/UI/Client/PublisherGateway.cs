using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Messaging;
using ClearMeasure.Bootcamp.UI.Shared;

namespace ClearMeasure.Bootcamp.UI.Client;

public class PublisherGateway(HttpClient httpClient, IConfiguration? configuration = null) : IPublisherGateway
{
    /// <summary>
    /// Legacy unversioned URL for the Blazor WASM single-API endpoint.
    /// </summary>
    public const string ApiRelativeUrl = WebServiceApiRoutes.LegacyRelativeUrl;

    /// <summary>
    /// Versioned URL using the current default API version in the path.
    /// </summary>
    public const string ApiRelativeUrlV1 = "api/v1.0/" + WebServiceApiRoutes.AbstractPathSegment;

    public async Task<WebServiceMessage?> Publish(IRemotableRequest request)
    {
        var message = new WebServiceMessage(request);
        return await SendToTopic(message);
    }

    public async Task Publish(IRemotableEvent @event)
    {
        var message = new WebServiceMessage(@event);
        await SendToTopic(message);
    }

    public virtual async Task<WebServiceMessage?> SendToTopic(WebServiceMessage message)
    {
        // Construct then assign Content so a Content factory throw cannot skip Dispose
        // (Qodana UsingStatementResourceInitialization).
        using var request = new HttpRequestMessage(HttpMethod.Post, ApiRelativeUrl);
        request.Content = JsonContent.Create(message);
        var key = configuration?["ApiKeyAuthentication:ValidationKey"];
        if (!string.IsNullOrWhiteSpace(key))
        {
            request.Headers.TryAddWithoutValidation(ApiKeyConstants.HeaderName, key.Trim());
        }

        var result = await httpClient.SendAsync(request);
        var json = await result.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<WebServiceMessage>(json);
    }
}