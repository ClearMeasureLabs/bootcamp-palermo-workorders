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
    public void ShouldReturnAvailableForOllama_WhenAzureApiKeyEmpty()
    {
        var result = ChatClientConfigValidator.Validate(new ChatClientConfig
        {
            AiOpenAiApiKey = "",
            AiOpenAiUrl = "",
            AiOpenAiModel = ""
        });

        result.IsAvailable.ShouldBeTrue();
        result.Message.ShouldContain(OllamaChatDefaults.DefaultModelId);
    }

    [Test]
    public void ShouldReturnUnavailable_WhenAzurePartiallyConfigured()
    {
        var result = ChatClientConfigValidator.Validate(new ChatClientConfig
        {
            AiOpenAiApiKey = "key",
            AiOpenAiUrl = "",
            AiOpenAiModel = ""
        });

        result.IsAvailable.ShouldBeFalse();
        result.Message.ShouldContain("partially configured");
        result.Message.ShouldContain("AI_OpenAI_Url");
        result.Message.ShouldContain("AI_OpenAI_Model");
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
    public async Task ShouldReportOllamaConfigured_WhenAzureApiKeyMissing()
    {
        var factory = new ChatClientFactory(new StubBus(available: false));

        var result = await factory.IsChatClientAvailable();

        result.IsAvailable.ShouldBeTrue();
        result.Message.ShouldContain(OllamaChatDefaults.DefaultModelId);
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
public class OllamaChatDefaultsTests
{
    [Test]
    public void ShouldUseQwenGsqRcoModelAndGpuSafeNumCtx()
    {
        OllamaChatDefaults.DefaultModelId.ShouldBe("qwen38-27b-gsq-rco");
        OllamaChatDefaults.MaxNumCtx.ShouldBe(49152);
    }

    [Test]
    public void WithCappedNumCtx_ShouldSetDefaultWhenMissing()
    {
        var options = OllamaChatDefaults.WithCappedNumCtx(null);

        options.AdditionalProperties.ShouldNotBeNull();
        options.AdditionalProperties[OllamaChatDefaults.NumCtxPropertyName].ShouldBe(49152);
    }

    [Test]
    public void WithCappedNumCtx_ShouldNotAcceptNumCtxAboveMax()
    {
        var incoming = new ChatOptions
        {
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                [OllamaChatDefaults.NumCtxPropertyName] = 65536
            }
        };

        var options = OllamaChatDefaults.WithCappedNumCtx(incoming);

        options.AdditionalProperties.ShouldNotBeNull();
        options.AdditionalProperties[OllamaChatDefaults.NumCtxPropertyName].ShouldBe(49152);
    }

    [Test]
    public void WithCappedNumCtx_ShouldKeepLowerRequestedNumCtx()
    {
        var incoming = new ChatOptions
        {
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                [OllamaChatDefaults.NumCtxPropertyName] = 8192
            }
        };

        var options = OllamaChatDefaults.WithCappedNumCtx(incoming);

        options.AdditionalProperties.ShouldNotBeNull();
        options.AdditionalProperties[OllamaChatDefaults.NumCtxPropertyName].ShouldBe(8192);
    }
}

[TestFixture]
public class ChatClientFactoryProviderTests
{
    [Test]
    public void BuildProviderChatClient_WhenApiKeyEmpty_UsesOllama()
    {
        var config = new ChatClientConfig
        {
            AiOpenAiApiKey = "",
            AiOpenAiUrl = "",
            AiOpenAiModel = ""
        };

        var client = ChatClientFactory.BuildProviderChatClient(config);

        FindClientOfType<OllamaChatClient>(client).ShouldNotBeNull();
        FindClientOfType<OllamaNumCtxChatClient>(client).ShouldNotBeNull();
    }

    [Test]
    public async Task GetChatClient_WhenApiKeyEmpty_WrapsOllamaInTracingClient()
    {
        var factory = new ChatClientFactory(new EmptyAzureKeyBus());

        var client = await factory.GetChatClient();

        client.ShouldBeOfType<TracingChatClient>();
        FindClientOfType<OllamaChatClient>(client).ShouldNotBeNull();
    }

    private static T? FindClientOfType<T>(IChatClient client) where T : class
    {
        while (true)
        {
            if (client is T match)
            {
                return match;
            }

            var inner = GetInnerClient(client);
            if (inner is null)
            {
                return null;
            }

            client = inner;
        }
    }

    private static IChatClient? GetInnerClient(IChatClient client)
    {
        var type = client.GetType();
        while (type is not null)
        {
            var field = type.GetField("_innerClient",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field?.GetValue(client) is IChatClient inner)
            {
                return inner;
            }

            var property = type.GetProperty("InnerClient",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);
            if (property?.GetValue(client) is IChatClient innerFromProperty)
            {
                return innerFromProperty;
            }

            type = type.BaseType;
        }

        return null;
    }

    private sealed class EmptyAzureKeyBus() : Bus(null!)
    {
        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is ChatClientConfigQuery)
            {
                var config = new ChatClientConfig
                {
                    AiOpenAiApiKey = "",
                    AiOpenAiUrl = "",
                    AiOpenAiModel = ""
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
