using System.Net;
using System.Text.Json;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class TimestampConverterIntegrationTests
{
    [Test]
    public async Task Get_Should_Return200Json_ForVersionedRoute()
    {
        await using var factory = new GrpcWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1.0/tools/timestamp-converter?value=0");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("unixTimeSeconds").GetInt64().ShouldBe(0L);
    }
}
