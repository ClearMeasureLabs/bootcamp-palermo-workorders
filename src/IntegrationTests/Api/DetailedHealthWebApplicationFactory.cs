using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// Hosts UI.Server in-process with SQLite in-memory so CI can exercise <c>/api/*</c> without LocalDB.
/// API key auth is enabled so anonymous <c>/api/health*</c> coverage is meaningful.
/// </summary>
public sealed class DetailedHealthWebApplicationFactory : WebApplicationFactory<UiServerWebApplicationMarker>
{
    private readonly SqliteConnection _database = new(
        $"Data Source=health-api-validation-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");

    /// <summary>Keeps the schema alive across host startup and request dependency scopes.</summary>
    public DetailedHealthWebApplicationFactory()
    {
        _database.Open();
    }

    /// <summary>Validation key configured for this factory when API key auth is enabled.</summary>
    public const string IntegrationApiKey = "integration-test-api-key";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Avoid appsettings.Development.json (LocalDB) overriding SQLite on Linux CI.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:SqlConnectionString", _database.ConnectionString);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlConnectionString"] = _database.ConnectionString,
                ["AI_OpenAI_ApiKey"] = "",
                ["AI_OpenAI_Url"] = "",
                ["AI_OpenAI_Model"] = "",
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = "",
                ["ApiKeyAuthentication:Enabled"] = "true",
                ["ApiKeyAuthentication:ValidationKey"] = IntegrationApiKey,
                ["ApiRateLimiting:Enabled"] = "true",
                ["ApiRateLimiting:PermitLimit"] = "100",
                ["ApiRateLimiting:WindowSeconds"] = "60",
                ["ApiRateLimiting:SegmentsPerWindow"] = "4",
                ["ApiRateLimiting:QueueLimit"] = "0",
                ["FeatureFlags:SampleFeatureA"] = "false",
                ["FeatureFlags:SampleFeatureB"] = "false"
            });
        });
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _database.Dispose();
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }
}
