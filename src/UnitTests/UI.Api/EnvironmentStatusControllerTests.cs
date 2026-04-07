using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class EnvironmentStatusControllerTests
{
    [Test]
    public void Get_Should_ReturnOk_WithExpectedShape()
    {
        var controller = new EnvironmentStatusController
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
        payload!.OsDescription.ShouldBe(System.Runtime.InteropServices.RuntimeInformation.OSDescription);
        payload.ProcessorCount.ShouldBe(Environment.ProcessorCount);
        payload.ClrVersion.ShouldBe(Environment.Version.ToString());
        payload.EnvironmentVariables.Count.ShouldBe(EnvironmentStatusResponseBuilder.DiagnosticEnvironmentVariableNames.Length);
    }

    [Test]
    public void Build_Should_RedactOrMarkUnset_When_VariablesPresentOrMissing()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Production",
            ["MISSING_FROM_LIST"] = "secret"
        };

        var payload = EnvironmentStatusResponseBuilder.Build(name =>
            map.TryGetValue(name, out var v) ? v : null);

        payload.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"].ShouldBe(EnvironmentStatusResponseBuilder.RedactedValue);
        payload.EnvironmentVariables["DOTNET_ENVIRONMENT"].ShouldBe("(not set)");
        map["MISSING_FROM_LIST"].ShouldBe("secret");
    }
}
