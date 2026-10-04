using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// Hosts UI.Server with SQLite shared in-memory so tests can seed the same database the app uses.
/// </summary>
public sealed class GrpcWebApplicationFactory : WebApplicationFactory<UiServerWebApplicationMarker>
{
    internal const string SqliteSharedMemoryConnectionString = "Data Source=grpc-workorders-integration;Mode=Memory;Cache=Shared";

    /// <summary>
    /// Separate shared DB for rate-limit + gRPC tests so unit tests keep an isolated bucket.
    /// </summary>
    internal const string RateLimitTestSqliteConnectionString = "Data Source=grpc-rate-limit-integration;Mode=Memory;Cache=Shared";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:SqlConnectionString", SqliteSharedMemoryConnectionString);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlConnectionString"] = SqliteSharedMemoryConnectionString,
                ["AI_OpenAI_ApiKey"] = "",
                ["AI_OpenAI_Url"] = "",
                ["AI_OpenAI_Model"] = "",
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = ""
            });
        });
    }
}
