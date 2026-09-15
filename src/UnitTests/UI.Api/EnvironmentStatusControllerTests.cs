using System.Runtime.InteropServices;
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
    private const string SecretValue = "unit-test-env-status-secret-value";

    [Test]
    public void Get_Should_ReturnJson_WithOsDescriptionProcessorCountClrVersionAndEnvVarNames()
    {
        var result = CreateController().Get();

        var payload = AssertOkPayload(result);
        payload.OsDescription.ShouldBe(RuntimeInformation.OSDescription);
        payload.ProcessorCount.ShouldBe(Environment.ProcessorCount);
        payload.ProcessorCount.ShouldBeGreaterThan(0);
        payload.ClrVersion.ShouldBe(Environment.Version.ToString());
        payload.ClrVersion.ShouldNotBeNullOrEmpty();
        payload.EnvironmentVariableNames.ShouldNotBeNull();
        payload.EnvironmentVariables.ShouldNotBeNull();
        payload.EnvironmentVariableNames.Count.ShouldBe(payload.EnvironmentVariables.Count);
    }

    [Test]
    public void Get_Should_OmitEnvironmentVariableValues_When_SecretEnvVarPresent()
    {
        using var probe = RedactionProbe.Install(SecretValue);

        var result = CreateController().Get();
        var payload = AssertOkPayload(result);

        payload.EnvironmentVariableNames.ShouldContain(EnvironmentStatusController.RedactionProbeVariableName);
        payload.EnvironmentVariables[EnvironmentStatusController.RedactionProbeVariableName]
            .ShouldBe(EnvironmentStatusController.RedactedValue);
        payload.EnvironmentVariables.Values.ShouldAllBe(value => value == EnvironmentStatusController.RedactedValue);
    }

    [Test]
    public void Get_Should_NotEchoSecretSubstrings_InAnyProperty()
    {
        using var probe = RedactionProbe.Install(SecretValue);

        var result = CreateController().Get();
        var content = result.ShouldBeOfType<ContentResult>();
        content.Content.ShouldNotBeNull();
        content.Content!.ShouldNotContain(SecretValue);
    }

    private static EnvironmentStatusController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    private static EnvironmentStatusResponse AssertOkPayload(IActionResult result)
    {
        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        content.Content.ShouldNotBeNull();
        var payload = JsonSerializer.Deserialize<EnvironmentStatusResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        return payload;
    }

    private sealed class RedactionProbe : IDisposable
    {
        private readonly string? _previous;

        private RedactionProbe(string? previous) => _previous = previous;

        public static RedactionProbe Install(string value)
        {
            var previous = Environment.GetEnvironmentVariable(
                EnvironmentStatusController.RedactionProbeVariableName);
            Environment.SetEnvironmentVariable(
                EnvironmentStatusController.RedactionProbeVariableName, value);
            return new RedactionProbe(previous);
        }

        public void Dispose() =>
            Environment.SetEnvironmentVariable(
                EnvironmentStatusController.RedactionProbeVariableName, _previous);
    }
}
