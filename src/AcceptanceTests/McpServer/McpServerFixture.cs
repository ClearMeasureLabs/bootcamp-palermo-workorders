using System.Diagnostics;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.IntegrationTests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;

namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

[SetUpFixture]
public class McpServerFixture
{
    private const string McpServerRelativeProjectDir = "../../../../McpServer";

    public static McpClient? McpClientInstance { get; private set; }
    public static IList<McpClientTool>? Tools { get; private set; }
    public static bool ServerAvailable { get; private set; }
    public static bool OllamaAvailable { get; private set; }
    private static readonly List<string> ServerErrors = new();

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        EnsureDatabaseInitialized();

        var connectionString = ResolveConnectionString();

        // Enable WAL mode for SQLite to allow concurrent reads/writes from
        // the test host process and the MCP server child process
        EnableSqliteWalMode(connectionString);

        // Release all test host pooled connections so the MCP server process
        // can access the database without contention
        TestHost.GetRequiredService<IDatabaseConfiguration>().ResetConnectionPool();

        await StartMcpServer(connectionString);
        await CheckOllamaAvailability();
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

    private static void EnsureDatabaseInitialized()
    {
        if (ServerFixture.DatabaseInitialized) return;

        using var context = TestHost.GetRequiredService<DbContext>();
        var isSqlite = context.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
        if (isSqlite)
        {
            context.Database.EnsureCreated();
        }

        new ZDataLoader().LoadData();
        TestContext.Out.WriteLine("McpServerFixture: ZDataLoader().LoadData() - complete");

        TestHost.GetRequiredService<IDatabaseConfiguration>().ResetConnectionPool();
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
            TestContext.Out.WriteLine($"McpServerFixture: SQLite WAL mode = {result}");
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"McpServerFixture: Failed to enable WAL mode: {ex.Message}");
        }
    }

    private static string ResolveConnectionString()
    {
        var configuration = TestHost.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("SqlConnectionString") ?? "";

        // For SQLite file-based databases, resolve to absolute path so the
        // MCP server child process can find the database file
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

        TestContext.Out.WriteLine($"McpServerFixture: connection string resolved");
        return connectionString;
    }

    private static async Task StartMcpServer(string connectionString)
    {
        var mcpServerProjectDir = Path.GetFullPath(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, McpServerRelativeProjectDir));

        TestContext.Out.WriteLine($"McpServerFixture: building MCP server at {mcpServerProjectDir}");

        // Pre-build so the stdio connection doesn't time out waiting for compilation
        var buildProcess = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{mcpServerProjectDir}\" --configuration Debug",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        if (buildProcess != null)
        {
            await buildProcess.WaitForExitAsync();
            if (buildProcess.ExitCode != 0)
            {
                var stderr = await buildProcess.StandardError.ReadToEndAsync();
                TestContext.Out.WriteLine($"McpServerFixture: build failed: {stderr}");
                ServerAvailable = false;
                return;
            }
        }

        TestContext.Out.WriteLine("McpServerFixture: build succeeded, starting MCP server via stdio");

        try
        {
            var transport = new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = "ChurchBulletin",
                Command = "dotnet",
                Arguments = ["run", "--no-build", "--project", mcpServerProjectDir],
                EnvironmentVariables = new Dictionary<string, string?>
                {
                    ["ConnectionStrings__SqlConnectionString"] = connectionString
                },
                StandardErrorLines = line =>
                {
                    ServerErrors.Add(line);
                    TestContext.Out.WriteLine($"[McpServer stderr] {line}");
                }
            });

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            McpClientInstance = await McpClient.CreateAsync(transport, cancellationToken: cts.Token);
            Tools = await McpClientInstance.ListToolsAsync(cancellationToken: cts.Token);

            TestContext.Out.WriteLine($"McpServerFixture: connected, {Tools.Count} tools discovered");
            ServerAvailable = true;
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"McpServerFixture: failed to start MCP server: {ex.Message}");
            ServerAvailable = false;
        }
    }

    private static async Task CheckOllamaAvailability()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await client.GetAsync("http://localhost:11434/");
            OllamaAvailable = response.IsSuccessStatusCode;
            TestContext.Out.WriteLine($"McpServerFixture: Ollama available = {OllamaAvailable}");
        }
        catch
        {
            OllamaAvailable = false;
            TestContext.Out.WriteLine("McpServerFixture: Ollama not available");
        }
    }
}
