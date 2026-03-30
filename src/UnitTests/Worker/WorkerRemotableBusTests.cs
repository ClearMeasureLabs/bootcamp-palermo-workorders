using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Http;
using Shouldly;
using Worker.Messaging;

namespace ClearMeasure.Bootcamp.UnitTests.Worker;

[TestFixture]
public class WorkerRemotableBusTests
{
    [Test]
    public async Task Send_WithRemotableRequest_PostsToClientBaseAddress()
    {
        var stubHandler = new StubHttpMessageHandler();
        var expectedStatus = HealthStatus.Healthy;
        var responseMessage = new WebServiceMessage(expectedStatus);
        stubHandler.SetResponse(JsonSerializer.Serialize(responseMessage));
        var baseUri = new Uri("https://api.example.test/api/blazor-wasm-single-api");
        using var httpClient = new HttpClient(stubHandler) { BaseAddress = baseUri };
        var bus = new RemotableBus(new StubHttpClientFactory(httpClient));

        var result = await bus.Send(new HealthCheckRemotableRequest());

        result.ShouldBe(expectedStatus);
        stubHandler.LastRequestUri.ShouldNotBeNull();
        stubHandler.LastRequestUri!.ShouldBe(baseUri);
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private string _responseContent = "";

        public Uri? LastRequestUri { get; private set; }

        public void SetResponse(string content)
        {
            _responseContent = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseContent)
            };
            return Task.FromResult(response);
        }
    }
}
