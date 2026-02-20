∩╗┐using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;

namespace ClearMeasure.Bootcamp.UI.Client;

public class PublisherGateway(HttpClient httpClient) : IPublisherGateway
{
    public const string ApiRelativeUrl = "api/blazor-wasm-single-api";

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
        
        if (!result.IsSuccessStatusCode)
        {
            var errorMessage = await result.Content.ReadAsStringAsync();
            throw new InvalidOperationException(errorMessage);
        }
        
        var json = await result.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<WebServiceMessage>(json);
    }
}
