using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Messaging;
using ClearMeasure.Bootcamp.UI.Shared;
using Microsoft.Extensions.Configuration;

namespace ClearMeasure.Bootcamp.UI.Client;

public class PublisherGateway(HttpClient httpClient, IConfiguration? configuration = null) : IPublisherGateway
{
    /// <summary>
    /// Path segment after <c>api/</c> (no leading slash). Used with versioned base <c>api/v1.0/{path}</c>.
    /// </summary>
    public const string ApiRelativePath = WebServiceApiRoutes.AbstractPathSegment;

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
        using var request = new HttpRequestMessage(HttpMethod.Post, ApiRelativeUrl)
        {
            Content = JsonContent.Create(message)
        };
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