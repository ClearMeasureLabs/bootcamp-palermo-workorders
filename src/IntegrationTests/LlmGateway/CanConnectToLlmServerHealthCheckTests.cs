using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.Extensions.Configuration;
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
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "AI_OpenAI_Url", "https://placeholder.openai.azure.com" },
                { "AI_OpenAI_Model", "gpt-4o" }
            })
            .Build();
        var chatClientFactory = TestHost.GetRequiredService<ChatClientFactory>();
        var logger = TestHost.GetRequiredService<ILogger<CanConnectToLlmServerHealthCheck>>();
        var healthCheck = new CanConnectToLlmServerHealthCheck(configuration, chatClientFactory, logger);
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
    public async Task CheckHealthAsync_WithMissingUrl_ReturnsUnhealthy()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "AI_OpenAI_ApiKey", "some-api-key" },
                { "AI_OpenAI_Model", "gpt-4o" }
            })
            .Build();
        var chatClientFactory = TestHost.GetRequiredService<ChatClientFactory>();
        var logger = TestHost.GetRequiredService<ILogger<CanConnectToLlmServerHealthCheck>>();
        var healthCheck = new CanConnectToLlmServerHealthCheck(configuration, chatClientFactory, logger);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("LlmGateway", healthCheck, null, null)
        };

        var result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNullOrEmpty();
        Console.WriteLine($"Status: {result.Status}, Description: {result.Description}");
    }
}
