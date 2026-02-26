using Microsoft.Extensions.Configuration;

namespace ClearMeasure.Bootcamp.IntegrationTests.LlmGateway;

public abstract class LlmTestBase : IntegratedTestBase
{
    [SetUp]
    public void SkipWhenLlmUnavailable()
    {
        var configuration = TestHost.GetRequiredService<IConfiguration>();
        var apiKey = configuration.GetValue<string>("AI_OpenAI_ApiKey");
        var url = configuration.GetValue<string>("AI_OpenAI_Url");
        var model = configuration.GetValue<string>("AI_OpenAI_Model");

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(url) || string.IsNullOrEmpty(model))
        {
            Assert.Ignore("No LLM provider configured. Set AI_OpenAI_ApiKey, AI_OpenAI_Url, and AI_OpenAI_Model to run LLM tests.");
        }
    }
}
