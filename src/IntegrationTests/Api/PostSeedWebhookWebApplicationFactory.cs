using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// SQLite shared-memory host for post-seed webhook HTTP integration tests.
/// </summary>
public sealed class PostSeedWebhookWebApplicationFactory : WebApplicationFactory<UiServerWebApplicationMarker>
{
    private readonly string _sqliteConnectionString;

    /// <param name="sqliteSharedDatabaseName">Distinct name per fixture so parallel tests do not share EF state.</param>
    public PostSeedWebhookWebApplicationFactory(string sqliteSharedDatabaseName = "post-seed-webhook-shared")
    {
        _sqliteConnectionString = $"Data Source={sqliteSharedDatabaseName};Mode=Memory;Cache=Shared";
    }

    internal string SqliteConnectionString => _sqliteConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:SqlConnectionString", _sqliteConnectionString);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlConnectionString"] = _sqliteConnectionString,
                ["AI_OpenAI_ApiKey"] = "",
                ["AI_OpenAI_Url"] = "",
                ["AI_OpenAI_Model"] = "",
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = "",
                ["ApiKeyAuthentication:Enabled"] = "false",
                ["ApiKeyAuthentication:ValidationKey"] = ""
            });
        });
    }
}
