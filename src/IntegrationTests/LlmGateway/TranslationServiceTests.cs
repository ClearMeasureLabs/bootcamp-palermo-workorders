using ClearMeasure.Bootcamp.LlmGateway;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.LlmGateway;

[TestFixture]
public class TranslationServiceTests : LlmTestBase
{
    [Test]
    public async Task ShouldTranslateTextToSpanish()
    {
        var factory = TestHost.GetRequiredService<ChatClientFactory>();
        var service = new TranslationService(factory);

        var result = await service.TranslateAsync("Fix the broken pipe", "es-ES");

        result.ShouldNotBeNullOrWhiteSpace();
        result.ShouldNotBe("Fix the broken pipe");
    }

    [Test]
    public async Task ShouldTranslateTextToGerman()
    {
        var factory = TestHost.GetRequiredService<ChatClientFactory>();
        var service = new TranslationService(factory);

        var result = await service.TranslateAsync("Organize Christmas Concert", "de-DE");

        result.ShouldNotBeNullOrWhiteSpace();
        result.ShouldNotBe("Organize Christmas Concert");
    }

    [Test]
    public async Task ShouldReturnOriginalTextForEnUS()
    {
        var factory = TestHost.GetRequiredService<ChatClientFactory>();
        var service = new TranslationService(factory);

        var result = await service.TranslateAsync("Hello world", "en-US");

        result.ShouldBe("Hello world");
    }
}
