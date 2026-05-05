using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// UI.Server test host with API key validation and an explicit SQLite connection string (shared-memory safe).
/// </summary>
public sealed class ApiKeyProtectedSqliteWebApplicationFactory : WebApplicationFactory<UiServerWebApplicationMarker>
{
    private readonly string _sqlConnectionString;

    public ApiKeyProtectedSqliteWebApplicationFactory(string sqlConnectionString)
    {
        _sqlConnectionString = sqlConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:SqlConnectionString", _sqlConnectionString);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlConnectionString"] = _sqlConnectionString,
                ["AI_OpenAI_ApiKey"] = "",
                ["AI_OpenAI_Url"] = "",
                ["AI_OpenAI_Model"] = "",
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = "",
                ["ApiKeyAuthentication:Enabled"] = "true",
                ["ApiKeyAuthentication:ValidationKey"] = ApiKeyProtectedWebApplicationFactory.TestApiKey
            });
        });
    }
}
