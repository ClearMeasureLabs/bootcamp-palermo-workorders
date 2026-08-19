using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// Hosts UI.Server for <c>/api/status/environment</c> integration tests with explicit diagnostics configuration.
/// </summary>
public sealed class EnvironmentStatusWebApplicationFactory : WebApplicationFactory<UiServerWebApplicationMarker>
{
    internal const string TestEnvironmentVariableName = "CB_ENV_STATUS_IT_VAR_8355";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Environment.SetEnvironmentVariable(TestEnvironmentVariableName, "integration-test-secret");
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:SqlConnectionString", "Data Source=:memory:");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlConnectionString"] = "Data Source=:memory:",
                ["AI_OpenAI_ApiKey"] = "",
                ["AI_OpenAI_Url"] = "",
                ["AI_OpenAI_Model"] = "",
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = "",
                ["ApiKeyAuthentication:Enabled"] = "false",
                ["ApiKeyAuthentication:ValidationKey"] = "",
                ["EnvironmentDiagnostics:VariableNames:0"] = TestEnvironmentVariableName,
                ["EnvironmentDiagnostics:VariableNames:1"] = "DATABASE_ENGINE"
            });
        });
    }
}
