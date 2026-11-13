using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;

namespace ClearMeasure.Bootcamp.UnitTests.Api;

/// <summary>
/// Hosts UI.Server with a retained shared SQLite database for exercising the Blazor WASM single-API pipeline.
/// </summary>
public sealed class WebServiceMessageValidationWebApplicationFactory : WebApplicationFactory<UiServerWebApplicationMarker>
{
    private readonly SqliteConnection _database = new(
        $"Data Source=single-api-validation-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");

    /// <summary>Keeps the shared database alive across the startup and request dependency scopes.</summary>
    public WebServiceMessageValidationWebApplicationFactory()
    {
        _database.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = ""
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
