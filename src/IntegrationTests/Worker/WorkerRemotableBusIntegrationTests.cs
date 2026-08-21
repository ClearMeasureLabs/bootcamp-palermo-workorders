using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Messaging;
using MediatR;
using Shouldly;
using Worker.Messaging;

namespace ClearMeasure.Bootcamp.IntegrationTests.Worker;

/// <summary>
/// HTTP-level integration coverage for Worker <see cref="RemotableBus"/>.
/// Kept out of TestHost's NServiceBus scanner via <c>ExcludeAssemblies("Worker.dll")</c>.
/// </summary>
[TestFixture]
public class WorkerRemotableBusIntegrationTests
{
    [Test]
    public async Task Send_ShouldRoundTripWebServiceMessageOverHttp()
    {
        var responsePayload = new EchoResponse("pong");
        using var handler = new RoundTripHandler(responsePayload);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://worker-test/") };
        var bus = new RemotableBus(httpClient, "remotable");

        var result = await bus.Send(new EchoRequest("ping"));

        result.Value.ShouldBe("pong");
        handler.ReceivedTypeName.ShouldNotBeNull();
        handler.ReceivedTypeName.ShouldContain(nameof(EchoRequest));
        handler.ReceivedBody.ShouldNotBeNull();
        handler.ReceivedBody.ShouldContain("ping");
    }

    private sealed class RoundTripHandler : HttpMessageHandler
    {
        private readonly object _responseBody;

        public RoundTripHandler(object responseBody)
        {
            _responseBody = responseBody;
        }

        public string? ReceivedTypeName { get; private set; }
        public string? ReceivedBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var incoming = await request.Content!.ReadFromJsonAsync<WebServiceMessage>(cancellationToken);
            ReceivedTypeName = incoming!.TypeName;
            ReceivedBody = incoming.Body;

            var outbound = new WebServiceMessage(_responseBody);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(outbound))
            };
        }
    }

    private record EchoRequest(string Value) : IRequest<EchoResponse>, IRemotableRequest;

    private record EchoResponse(string Value);
}
