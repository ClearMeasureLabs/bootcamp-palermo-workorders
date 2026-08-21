using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.LlmGateway;

[TestFixture]
public class CanConnectToLlmServerHealthCheckTests
{
    [Test]
    public async Task CheckHealthAsync_WhenUnavailable_ReturnsDegraded()
    {
        var healthCheck = CreateHealthCheck(new StubChatClientFactory(available: false));

        var result = await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("missing");
    }

    [Test]
    public async Task CheckHealthAsync_WhenChatSucceeds_ReturnsHealthy()
    {
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, "OK")]);
        var healthCheck = CreateHealthCheck(new StubChatClientFactory(available: true, response: response));

        var result = await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Test]
    public async Task CheckHealthAsync_WhenChatEmpty_ReturnsDegraded()
    {
        var healthCheck = CreateHealthCheck(
            new StubChatClientFactory(available: true, response: new ChatResponse([])));

        var result = await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Test]
    public async Task CheckHealthAsync_WhenChatThrows_ReturnsUnhealthy()
    {
        var healthCheck = CreateHealthCheck(
            new StubChatClientFactory(available: true, failOnGetResponse: true));

        var result = await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("Chat client connection failed");
    }

    private static CanConnectToLlmServerHealthCheck CreateHealthCheck(ChatClientFactory factory) =>
        new(factory, NullLogger<CanConnectToLlmServerHealthCheck>.Instance);

    private static HealthCheckContext CreateContext(CanConnectToLlmServerHealthCheck healthCheck) =>
        new()
        {
            Registration = new HealthCheckRegistration("LlmGateway", healthCheck, null, null)
        };

    private sealed class StubChatClientFactory(
        bool available,
        ChatResponse? response = null,
        bool failOnGetResponse = false) : ChatClientFactory(null!)
    {
        public override Task<ChatClientAvailabilityResult> IsChatClientAvailable() =>
            Task.FromResult(available
                ? new ChatClientAvailabilityResult(true, "configured")
                : new ChatClientAvailabilityResult(false, "missing configuration"));

        public override Task<IChatClient> GetChatClient() =>
            Task.FromResult<IChatClient>(new StubHealthChatClient(response, failOnGetResponse));
    }

    private sealed class StubHealthChatClient(ChatResponse? response, bool failOnGetResponse) : IChatClient
    {
        public void Dispose()
        {
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (failOnGetResponse)
            {
                throw new InvalidOperationException("probe-failed");
            }

            return Task.FromResult(response ?? new ChatResponse([]));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
    }
}
