using System.Net;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
[NonParallelizable]
public class NeedsRebootHealthCheckTests : AcceptanceTestBase
{
    protected override bool RequiresBrowser => false;

    private HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        return new HttpClient(handler)
        {
            BaseAddress = new Uri(ServerFixture.ApplicationBaseUrl)
        };
    }

    [TearDown]
    public async Task ResetNeedsReboot()
    {
        using var client = CreateHttpClient();
        await client.GetAsync("/_demo/setneedsreboot/false");
    }

    [Test, Retry(2)]
    public async Task SetNeedsRebootTrue_HealthCheckReturnsUnhealthy()
    {
        using var client = CreateHttpClient();

        var setResponse = await client.GetAsync("/_demo/setneedsreboot/true");
        setResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var setText = await setResponse.Content.ReadAsStringAsync();
        setText.ShouldContain("True");

        var healthResponse = await client.GetAsync("/_healthcheck");
        var healthBody = await healthResponse.Content.ReadAsStringAsync();
        healthBody.ShouldContain("Unhealthy", Case.Insensitive);
    }

    [Test, Retry(2)]
    public async Task SetNeedsRebootFalse_HealthCheckDoesNotReturnUnhealthy()
    {
        using var client = CreateHttpClient();

        await client.GetAsync("/_demo/setneedsreboot/true");
        var resetResponse = await client.GetAsync("/_demo/setneedsreboot/false");
        resetResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var resetText = await resetResponse.Content.ReadAsStringAsync();
        resetText.ShouldContain("False");

        var healthResponse = await client.GetAsync("/_healthcheck");
        var healthBody = await healthResponse.Content.ReadAsStringAsync();
        healthBody.ShouldNotContain("Unhealthy", Case.Insensitive);
    }

    [Test, Retry(2)]
    public async Task SetNeedsRebootRoute_ReturnsSummaryText()
    {
        using var client = CreateHttpClient();

        var trueResponse = await client.GetAsync("/_demo/setneedsreboot/true");
        trueResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var trueText = await trueResponse.Content.ReadAsStringAsync();
        trueText.ShouldBe("NeedsReboot set to True");

        var falseResponse = await client.GetAsync("/_demo/setneedsreboot/false");
        falseResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var falseText = await falseResponse.Content.ReadAsStringAsync();
        falseText.ShouldBe("NeedsReboot set to False");
    }

    [Test, Retry(2)]
    public async Task SetNeedsRebootTrue_DetailedHealthCheckShowsCorruptionMessage()
    {
        using var client = CreateHttpClient();

        await client.GetAsync("/_demo/setneedsreboot/true");

        var detailedResponse = await client.GetAsync("/_healthcheck/detailed");
        var detailedBody = await detailedResponse.Content.ReadAsStringAsync();
        detailedBody.ShouldContain("memory is corrupted");
        detailedBody.ShouldContain("Restart process");
    }
}
