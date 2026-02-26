using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.LlmGateway;
using MediatR;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.LlmGateway;

[TestFixture]
public class CanConnectToLlmServerHealthCheckTests : LlmTestBase
{
    [Test]
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
    public async Task CheckHealthAsync_WithMissingApiKey_ReturnsDegraded()
    {
        var factory = CreateFactoryWithConfig(apiKey: null, url: "https://placeholder.openai.azure.com", model: "gpt-4o");
        var logger = TestHost.GetRequiredService<ILogger<CanConnectToLlmServerHealthCheck>>();
        var healthCheck = new CanConnectToLlmServerHealthCheck(factory, logger);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("LlmGateway", healthCheck, null, null)
        };

        var result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotBeNullOrEmpty();
        Console.WriteLine($"Status: {result.Status}, Description: {result.Description}");
    }

    [Test]
    public async Task CheckHealthAsync_WithMissingUrl_ReturnsDegraded()
    {
        var factory = CreateFactoryWithConfig(apiKey: "some-api-key", url: null, model: "gpt-4o");
        var logger = TestHost.GetRequiredService<ILogger<CanConnectToLlmServerHealthCheck>>();
        var healthCheck = new CanConnectToLlmServerHealthCheck(factory, logger);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("LlmGateway", healthCheck, null, null)
        };

        var result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotBeNullOrEmpty();
        Console.WriteLine($"Status: {result.Status}, Description: {result.Description}");
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
}
