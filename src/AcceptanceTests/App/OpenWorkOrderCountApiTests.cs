using System.Net;
using System.Text.Json;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class OpenWorkOrderCountApiTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldReturnOpenCountJson_WhenCalledAnonymously()
    {
        var baseUrl = new Uri(Page.Url).GetLeftPart(UriPartial.Authority);
        var response = await Page.APIRequest.GetAsync($"{baseUrl}/api/tools/open-work-order-count");

        response.Status.ShouldBe((int)HttpStatusCode.OK);
        var json = await response.TextAsync();
        using var document = JsonDocument.Parse(json);
        document.RootElement.TryGetProperty("open", out var open).ShouldBeTrue();
        open.ValueKind.ShouldBe(JsonValueKind.Number);
        open.GetInt32().ShouldBeGreaterThanOrEqualTo(0);
    }
}
