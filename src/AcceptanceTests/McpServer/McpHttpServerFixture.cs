using ClearMeasure.Bootcamp.AcceptanceTests;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.IntegrationTests;
using ModelContextProtocol.Client;

namespace ClearMeasure.Bootcamp.McpAcceptanceTests;

[SetUpFixture]
public class McpHttpServerFixture
{
    public static McpClient? McpClientInstance { get; private set; }
    public static IList<McpClientTool>? Tools { get; private set; }
    public static bool ServerAvailable { get; private set; }

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        ServerFixture.InitializeDatabaseOnce();

        var connectionString = ResolveConnectionString();
        EnableSqliteWalMode(connectionString);
        TestHost.GetRequiredService<IDatabaseConfiguration>().ResetConnectionPool();

        // Wait for UI.Server to start (started by ServerFixture in a sibling namespace)
        await WaitForServerFixture();

        if (string.IsNullOrEmpty(ServerFixture.ApplicationBaseUrl))
        {
            TestContext.Out.WriteLine("McpHttpServerFixture: ServerFixture.ApplicationBaseUrl is not set, skipping MCP connection");
            ServerAvailable = false;
            return;
        }

        await ConnectToMcpEndpoint();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (McpClientInstance != null)
        {
            await McpClientInstance.DisposeAsync();
            McpClientInstance = null;
        }
    }

    private static async Task WaitForServerFixture()
    {
        // ServerFixture runs as a [SetUpFixture] in a sibling namespace.
        // NUnit doesn't guarantee ordering between sibling namespace fixtures,
        // so poll until the server is reachable or timeout.
        var baseUrl = ServerFixture.ApplicationBaseUrl;
        if (string.IsNullOrEmpty(baseUrl))
        {
            TestContext.Out.WriteLine("McpHttpServerFixture: ApplicationBaseUrl not yet set, waiting for ServerFixture...");
            var deadline = DateTime.UtcNow.AddSeconds(90);
            while (DateTime.UtcNow < deadline)
            {
                baseUrl = ServerFixture.ApplicationBaseUrl;
                if (!string.IsNullOrEmpty(baseUrl))
                    break;
                await Task.Delay(500);
            }
        }

        if (string.IsNullOrEmpty(baseUrl))
        {
            TestContext.Out.WriteLine("McpHttpServerFixture: timed out waiting for ServerFixture");
            return;
        }

        TestContext.Out.WriteLine($"McpHttpServerFixture: ApplicationBaseUrl = {baseUrl}, waiting for server to respond...");

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        var deadline2 = DateTime.UtcNow.AddSeconds(90);

        while (DateTime.UtcNow < deadline2)
        {
            try
            {
                var response = await httpClient.GetAsync(baseUrl);
                if (response.IsSuccessStatusCode)
                {
                    TestContext.Out.WriteLine("McpHttpServerFixture: server is reachable");
                    return;
                }
            }
            catch
            {
                // Server not ready yet
            }

            await Task.Delay(1000);
        }

        TestContext.Out.WriteLine("McpHttpServerFixture: timed out waiting for server to respond");
    }

    private static void EnableSqliteWalMode(string connectionString)
    {
        if (!connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA journal_mode=WAL;";
            var result = command.ExecuteScalar();
            TestContext.Out.WriteLine($"McpHttpServerFixture: SQLite WAL mode = {result}");
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"McpHttpServerFixture: Failed to enable WAL mode: {ex.Message}");
        }
    }

    private static string ResolveConnectionString()
    {
        var configuration = TestHost.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var connectionString = configuration.GetConnectionString("SqlConnectionString") ?? "";

        if (connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase)
            && !connectionString.Contains(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            var dbPath = connectionString["Data Source=".Length..].Trim();
            var semicolonIndex = dbPath.IndexOf(';');
            if (semicolonIndex >= 0) dbPath = dbPath[..semicolonIndex];

            if (!Path.IsPathRooted(dbPath))
            {
                var absolutePath = Path.GetFullPath(dbPath);
                connectionString = $"Data Source={absolutePath}";
            }
        }

        TestContext.Out.WriteLine("McpHttpServerFixture: connection string resolved");
        return connectionString;
    }

    private static async Task ConnectToMcpEndpoint()
    {
        try
        {
            var mcpUrl = ServerFixture.ApplicationBaseUrl + "/mcp";
            TestContext.Out.WriteLine($"McpHttpServerFixture: connecting to {mcpUrl}");

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            var httpClient = new HttpClient(handler);

            var transportOptions = new HttpClientTransportOptions
            {
                Endpoint = new Uri(mcpUrl),
                Name = "ChurchBulletin-HTTP"
            };

            var transport = new HttpClientTransport(transportOptions, httpClient);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            McpClientInstance = await McpClient.CreateAsync(transport, cancellationToken: cts.Token);
            Tools = await McpClientInstance.ListToolsAsync(cancellationToken: cts.Token);

            TestContext.Out.WriteLine($"McpHttpServerFixture: connected via HTTP, {Tools.Count} tools discovered");
            ServerAvailable = true;
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"McpHttpServerFixture: failed to connect to MCP endpoint: {ex.Message}");
            ServerAvailable = false;
        }
    }
}
