using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.LlmGateway;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.LlmGateway;

[TestFixture]
public class CanConnectToLlmServerHealthCheckTests : LlmTestBase
{
    [Test]
    [LlmTest]
    public async Task CheckHealthAsync_WithCurrentConfiguration_ReturnsResult()
    {
        var healthCheck = TestHost.GetRequiredService<CanConnectToLlmServerHealthCheck>();
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("LlmGateway", healthCheck, null, null)
        };

        var result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBeOneOf(HealthStatus.Healthy, HealthStatus.Degraded);
        Console.WriteLine($"Status: {result.Status}, Description: {result.Description}");
    }

    [Test]
    public async Task IsChatClientAvailable_WithMissingApiKey_UsesLocalOllama()
    {
        var factory = CreateFactoryWithConfig(apiKey: null, url: "https://placeholder.openai.azure.com", model: "gpt-4o");

        var availability = await factory.IsChatClientAvailable();

        availability.IsAvailable.ShouldBeTrue();
        availability.Message.ShouldContain("Ollama");
        availability.Message.ShouldContain(OllamaChatDefaults.DefaultModelId);
    }

    [Test]
    public async Task CheckHealthAsync_WithMissingUrl_ReturnsHealthyWithInfo()
    {
        var factory = CreateFactoryWithConfig(apiKey: "some-api-key", url: null, model: "gpt-4o");
        var healthCheck = CreateHealthCheck(factory);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("LlmGateway", healthCheck, null, null)
        };

        var result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldNotBeNullOrEmpty();
        result.Description.ShouldContain("AI_OpenAI_Url");
        result.Description.ShouldContain("not enabled in this environment");
        Console.WriteLine($"Status: {result.Status}, Description: {result.Description}");
    }

    [Test]
    public async Task CheckHealthAsync_TwoCallsWithinWindow_ProducesOnlyOneModelCall()
    {
        var stubFactory = new CountingChatClientFactory(
            new ChatClientAvailabilityResult(true, "configured"),
            new ChatResponse([new ChatMessage(ChatRole.Assistant, "OK")]));
        var cache = new LlmHealthCheckCache();
        var opts = Options.Create(new LlmHealthCheckOptions { CacheDuration = TimeSpan.FromMinutes(5) });
        var logger = TestHost.GetRequiredService<ILogger<CanConnectToLlmServerHealthCheck>>();
        var healthCheck = new CanConnectToLlmServerHealthCheck(stubFactory, cache, opts, logger);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("LlmGateway", healthCheck, null, null)
        };

        var first = await healthCheck.CheckHealthAsync(context);
        var second = await healthCheck.CheckHealthAsync(context);

        stubFactory.GetChatClientCallCount.ShouldBe(1);
        second.Data["cached"].ShouldBe(true);
        Console.WriteLine($"First: {first.Status}, Second: {second.Status}, cached={second.Data["cached"]}");
    }

    [Test]
    public async Task CheckHealthAsync_CachedResult_ContainsCacheDataKeys()
    {
        var stubFactory = new CountingChatClientFactory(
            new ChatClientAvailabilityResult(true, "configured"),
            new ChatResponse([new ChatMessage(ChatRole.Assistant, "OK")]));
        var cache = new LlmHealthCheckCache();
        var opts = Options.Create(new LlmHealthCheckOptions { CacheDuration = TimeSpan.FromMinutes(5) });
        var logger = TestHost.GetRequiredService<ILogger<CanConnectToLlmServerHealthCheck>>();
        var healthCheck = new CanConnectToLlmServerHealthCheck(stubFactory, cache, opts, logger);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("LlmGateway", healthCheck, null, null)
        };

        await healthCheck.CheckHealthAsync(context);   // populate cache
        var result = await healthCheck.CheckHealthAsync(context); // cache hit

        result.Data.ContainsKey("cached").ShouldBeTrue();
        result.Data.ContainsKey("cache_age_seconds").ShouldBeTrue();
        result.Data["cached"].ShouldBe(true);
        ((double)result.Data["cache_age_seconds"]).ShouldBeGreaterThanOrEqualTo(0);
    }

    private CanConnectToLlmServerHealthCheck CreateHealthCheck(ChatClientFactory factory)
    {
        var logger = TestHost.GetRequiredService<ILogger<CanConnectToLlmServerHealthCheck>>();
        var opts = Options.Create(new LlmHealthCheckOptions());
        return new CanConnectToLlmServerHealthCheck(factory, new LlmHealthCheckCache(), opts, logger);
    }

    private static ChatClientFactory CreateFactoryWithConfig(string? apiKey, string? url, string? model)
    {
        var stubBus = new StubConfigBus(new ChatClientConfig
        {
            AiOpenAiApiKey = apiKey,
            AiOpenAiUrl = url,
            AiOpenAiModel = model
        });
        return new ChatClientFactory(stubBus);
    }

    private class StubConfigBus(ChatClientConfig config) : IBus
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is ChatClientConfigQuery)
            {
                return Task.FromResult((TResponse)(object)config);
            }
            throw new NotSupportedException();
        }

        public Task<object?> Send(object request) => throw new NotSupportedException();
        public Task Publish(INotification notification) => throw new NotSupportedException();
    }

    private sealed class CountingChatClientFactory(
        ChatClientAvailabilityResult availability,
        ChatResponse response) : ChatClientFactory(null!)
    {
        public int GetChatClientCallCount;

        public override Task<ChatClientAvailabilityResult> IsChatClientAvailable() =>
            Task.FromResult(availability);

        public override Task<IChatClient> GetChatClient()
        {
            GetChatClientCallCount++;
            return Task.FromResult<IChatClient>(new StubChatClient(response));
        }
    }

    private sealed class StubChatClient(ChatResponse response) : IChatClient
    {
        public void Dispose() { }

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
