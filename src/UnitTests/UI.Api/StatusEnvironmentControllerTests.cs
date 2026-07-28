using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class StatusEnvironmentControllerTests
{
    [Test]
    public void Get_Should_ReturnJson_WithExpectedShape_When_Called()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "UnitTestEnv"
            })
            .Build();
        var controller = new StatusEnvironmentController(configuration)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<EnvironmentStatusResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.OsDescription.ShouldNotBeNullOrWhiteSpace();
        payload.ProcessorCount.ShouldBeGreaterThan(0);
        payload.ClrVersion.ShouldNotBeNullOrWhiteSpace();
        payload.EnvironmentVariables.Count.ShouldBe(10);
        payload.EnvironmentVariables.ShouldContainKey("ASPNETCORE_ENVIRONMENT");
        payload.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"]
            .ShouldBe(EnvironmentStatusResponseBuilder.RedactedEnvironmentVariableValue);
    }
}
