using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Messaging;

namespace ClearMeasure.Bootcamp.UI.Client;

public class PublisherGateway(HttpClient httpClient) : IPublisherGateway
{
    /// <summary>
    /// Typed client name for <see cref="IHttpClientFactory"/> registration.
    /// </summary>
    public const string HttpClientName = nameof(PublisherGateway);
    /// <summary>
    /// Path segment after <c>api/</c> (no leading slash). Used with versioned base <c>api/v1.0/{path}</c>.
    /// </summary>
    public const string ApiRelativePath = "blazor-wasm-single-api";

    /// <summary>
    /// Legacy unversioned URL for the Blazor WASM single-API endpoint.
    /// </summary>
    public const string ApiRelativeUrl = "api/" + ApiRelativePath;

    /// <summary>
    /// Versioned URL using the current default API version in the path.
    /// </summary>
    public const string ApiRelativeUrlV1 = "api/v1.0/" + ApiRelativePath;

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
        HttpContent content = new StringContent(message.GetJson());
        var result = await httpClient.PostAsJsonAsync(ApiRelativeUrl, message);
        var json = await result.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<WebServiceMessage>(json);
    }
}