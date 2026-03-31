using System.Net;
using System.Net.Http.Json;
using ClearMeasure.Bootcamp.Core.Messaging;
using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.UI.Client;
using ClearMeasure.Bootcamp.UI.Server.Notifications;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// Verifies the WebSocket route and related HTTP wiring in the Testing host.
/// Full in-process WebSocket frame tests against <see cref="Microsoft.AspNetCore.TestHost.TestServer"/> are unreliable
/// (connect/receive can block indefinitely in CI); real socket behavior is covered by <see cref="ServerRealtimeBusTests"/>
/// and manual or browser-based checks when the Blazor client subscribes.
/// </summary>
[TestFixture]
public class RealtimeNotificationWebSocketIntegrationTests
{
    private const string ConnectionCountPath = "/_test/realtime/connection-count";

    private DetailedHealthWebApplicationFactory? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new DetailedHealthWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Should_RejectOrCloseConnection_When_RequestIsNotWebSocket()
    {
        var response = await _client!.GetAsync(RealtimeNotificationWebSocketMiddleware.Path)
            .ConfigureAwait(false);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_ExposeConnectionCount_When_TestingHostRuns()
    {
        var response = await _client!.GetAsync(ConnectionCountPath).ConfigureAwait(false);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        json.ShouldContain("count");
    }

    [Test]
    public async Task Should_AcceptPublisherGatewayPost_When_RemotableEventPublished()
    {
        var loginEvent = new UserLoggedInEvent("integration-ws-user");
        var postResponse = await _client!.PostAsJsonAsync(
                PublisherGateway.ApiRelativeUrl,
                new WebServiceMessage(loginEvent))
            .ConfigureAwait(false);
        postResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_StartupWithoutError_When_WebSocketFeatureIsRegistered()
    {
        var health = await _client!.GetAsync("/api/health").ConfigureAwait(false);
        health.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
