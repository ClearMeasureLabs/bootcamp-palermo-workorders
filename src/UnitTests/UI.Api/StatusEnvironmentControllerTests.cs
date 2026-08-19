using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class StatusEnvironmentControllerTests
{
    private const string TestVar1 = "CB_ENV_STATUS_TEST_VAR1_8355";
    private const string TestVar2 = "CB_ENV_STATUS_TEST_VAR2_8355";
    private const string SecretVar1 = "secret-value-1";
    private const string SecretVar2 = "secret-value-2";

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        Environment.SetEnvironmentVariable(TestVar1, null);
        Environment.SetEnvironmentVariable(TestVar2, null);
    }

    [Test]
    public void Get_Should_ReturnJson_WithOsProcessorAndClrVersion_When_Called()
    {
        var options = Options.Create(new EnvironmentDiagnosticsOptions { VariableNames = [] });
        var controller = CreateController(options);

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
        payload.ClrVersion.ShouldBe(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
    }

    [Test]
    public void Get_Should_IncludeWeak_ETag_When_Called()
    {
        var options = Options.Create(new EnvironmentDiagnosticsOptions { VariableNames = [] });
        var controller = CreateController(options);

        var result = controller.Get();

        result.ShouldBeOfType<ContentResult>();
        var etag = controller.Response.Headers.ETag.ToString();
        etag.ShouldNotBeNullOrEmpty();
        etag.ShouldStartWith("W/");
    }

    [Test]
    public void Get_Should_RedactEnvironmentVariables_When_AllowedVariablesAreSet()
    {
        Environment.SetEnvironmentVariable(TestVar1, SecretVar1);
        Environment.SetEnvironmentVariable(TestVar2, SecretVar2);
        var options = Options.Create(new EnvironmentDiagnosticsOptions
        {
            VariableNames = [TestVar1, TestVar2]
        });
        var controller = CreateController(options);

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<EnvironmentStatusResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.EnvironmentVariables.ShouldContainKey(TestVar1);
        payload.EnvironmentVariables.ShouldContainKey(TestVar2);
        payload.EnvironmentVariables[TestVar1].ShouldBe(EnvironmentStatusBuilder.RedactedValue);
        payload.EnvironmentVariables[TestVar2].ShouldBe(EnvironmentStatusBuilder.RedactedValue);
        content.Content!.ShouldNotContain(SecretVar1);
        content.Content!.ShouldNotContain(SecretVar2);
    }

    [Test]
    public void Get_Should_OmitUnsetEnvironmentVariables_When_KeyNotInEnvironment()
    {
        Environment.SetEnvironmentVariable(TestVar1, null);
        var options = Options.Create(new EnvironmentDiagnosticsOptions
        {
            VariableNames = [TestVar1, TestVar2]
        });
        var controller = CreateController(options);

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<EnvironmentStatusResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.EnvironmentVariables.ShouldNotContainKey(TestVar1);
        payload.EnvironmentVariables.ShouldNotContainKey(TestVar2);
    }

    [Test]
    public void Get_Should_OmitEnvironmentVariables_When_NotInAllowlist()
    {
        Environment.SetEnvironmentVariable(TestVar1, SecretVar1);
        var options = Options.Create(new EnvironmentDiagnosticsOptions { VariableNames = [] });
        var controller = CreateController(options);

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<EnvironmentStatusResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.EnvironmentVariables.ShouldNotContainKey(TestVar1);
        content.Content!.ShouldNotContain(SecretVar1);
    }

    private static StatusEnvironmentController CreateController(IOptions<EnvironmentDiagnosticsOptions> options)
    {
        return new StatusEnvironmentController(options)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }
}
