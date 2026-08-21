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
    public async Task CheckHealthAsync_WhenNotConfigured_ReturnsHealthyWithInfo()
    {
        var healthCheck = CreateHealthCheck(new StubChatClientFactory(
            new ChatClientAvailabilityResult(false, "missing configuration")));

        var result = await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("missing");
        result.Description.ShouldContain("not enabled in this environment");
    }

    [Test]
    public async Task CheckHealthAsync_WhenChatSucceeds_ReturnsHealthy()
    {
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, "OK")]);
        var healthCheck = CreateHealthCheck(new StubChatClientFactory(
            new ChatClientAvailabilityResult(true, "configured"),
            response));

        var result = await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Test]
    public async Task CheckHealthAsync_WhenChatEmpty_ReturnsDegraded()
    {
        var healthCheck = CreateHealthCheck(new StubChatClientFactory(
            new ChatClientAvailabilityResult(true, "configured"),
            new ChatResponse([])));

        var result = await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Test]
    public async Task CheckHealthAsync_WhenChatThrows_ReturnsUnhealthy()
    {
        var healthCheck = CreateHealthCheck(new ThrowingChatClientFactory());

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
        ChatClientAvailabilityResult availability,
        ChatResponse? response = null) : ChatClientFactory(null!)
    {
        public override Task<ChatClientAvailabilityResult> IsChatClientAvailable() =>
            Task.FromResult(availability);

        public override Task<IChatClient> GetChatClient() =>
            Task.FromResult<IChatClient>(new StubHealthChatClient(response ?? new ChatResponse([])));
    }

    private sealed class ThrowingChatClientFactory() : ChatClientFactory(null!)
    {
        public override Task<ChatClientAvailabilityResult> IsChatClientAvailable() =>
            Task.FromResult(new ChatClientAvailabilityResult(true, "configured"));

        public override Task<IChatClient> GetChatClient() =>
            throw new InvalidOperationException("probe-failed");
    }

    private sealed class StubHealthChatClient(ChatResponse response) : IChatClient
    {
        public void Dispose()
        {
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(response);

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
    }
}
