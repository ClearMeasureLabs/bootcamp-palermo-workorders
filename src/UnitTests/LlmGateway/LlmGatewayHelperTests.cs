using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Shared;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.LlmGateway;

[TestFixture]
public class ChatClientConfigValidatorTests
{
    [Test]
    public void ShouldReturnUnavailable_WhenConfigMissingValues()
    {
        var result = ChatClientConfigValidator.Validate(new ChatClientConfig
        {
            AiOpenAiApiKey = "",
            AiOpenAiUrl = "",
            AiOpenAiModel = ""
        });

        result.IsAvailable.ShouldBeFalse();
        result.Message.ShouldContain("AI_OpenAI_ApiKey");
    }

    [Test]
    public void ShouldReturnAvailable_WhenConfigComplete()
    {
        var result = ChatClientConfigValidator.Validate(new ChatClientConfig
        {
            AiOpenAiApiKey = "key",
            AiOpenAiUrl = "https://example.com",
            AiOpenAiModel = "gpt-4"
        });

        result.IsAvailable.ShouldBeTrue();
    }
}

[TestFixture]
public class ChatClientFactoryAvailabilityTests
{
    [Test]
    public async Task ShouldReportMissingConfiguration_WhenEnvironmentValuesMissing()
    {
        var factory = new ChatClientFactory(new StubBus(available: false));

        var result = await factory.IsChatClientAvailable();

        result.IsAvailable.ShouldBeFalse();
        result.Message.ShouldContain("AI_OpenAI_ApiKey");
    }

    [Test]
    public async Task ShouldReportConfigured_WhenEnvironmentValuesPresent()
    {
        var factory = new ChatClientFactory(new StubBus(available: true));

        var result = await factory.IsChatClientAvailable();

        result.IsAvailable.ShouldBeTrue();
    }

    private sealed class StubBus(bool available) : Bus(null!)
    {
        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is ChatClientConfigQuery)
            {
                var config = new ChatClientConfig
                {
                    AiOpenAiApiKey = available ? "test-key" : "",
                    AiOpenAiUrl = available ? "https://test.openai.azure.com" : "",
                    AiOpenAiModel = available ? "gpt-4" : ""
                };
                return Task.FromResult((TResponse)(object)config);
            }

            throw new NotImplementedException($"Unhandled request type: {request.GetType().Name}");
        }
    }
}

[TestFixture]
public class LlmHealthEvaluatorTests
{
    [Test]
    public void ShouldReturnHealthyWithInfo_WhenAvailabilityMissing()
    {
        var result = LlmHealthEvaluator.FromAvailability(
            new ChatClientAvailabilityResult(false, "missing config"));

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("not enabled in this environment");
    }

    [Test]
    public void ShouldReturnHealthy_WhenAvailable()
    {
        var result = LlmHealthEvaluator.FromAvailability(
            new ChatClientAvailabilityResult(true, "configured"));

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldBe("configured");
    }

    [Test]
    public void ShouldReturnHealthy_WhenResponseHasMessages()
    {
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, "OK")]);

        LlmHealthEvaluator.FromChatResponse(response).Status.ShouldBe(HealthStatus.Healthy);
    }

    [Test]
    public void ShouldReturnDegraded_WhenResponseEmpty()
    {
        var response = new ChatResponse([]);

        LlmHealthEvaluator.FromChatResponse(response).Status.ShouldBe(HealthStatus.Degraded);
    }
}

[TestFixture]
public class TranslationGuardTests
{
    [Test]
    public void ShouldReturnOriginal_WhenEnglishRequested()
    {
        TranslationGuard.ShouldReturnOriginal("hello", "en-US").ShouldBeTrue();
    }

    [Test]
    public void ShouldReturnOriginal_WhenLanguageCodeInvalid()
    {
        TranslationGuard.ShouldReturnOriginal("hello", "bad code").ShouldBeTrue();
    }
}
