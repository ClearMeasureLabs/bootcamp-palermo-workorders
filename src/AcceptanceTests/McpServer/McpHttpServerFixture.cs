using System.Diagnostics;
using ClearMeasure.Bootcamp.AcceptanceTests;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.IntegrationTests;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;

namespace ClearMeasure.Bootcamp.McpAcceptanceTests;

[SetUpFixture]
public class McpHttpServerFixture
{
    private const string McpServerRelativeProjectDir = "../../../../McpServer";
    private const string ServerUrl = "http://localhost:3001";

    private static string BuildConfiguration =>
        AppDomain.CurrentDomain.BaseDirectory.Contains(
            Path.DirectorySeparatorChar + "Release" + Path.DirectorySeparatorChar)
            ? "Release"
            : "Debug";

    public static McpClient? McpClientInstance { get; private set; }
    public static IList<McpClientTool>? Tools { get; private set; }
    public static bool ServerAvailable { get; private set; }
    private static Process? _serverProcess;
    private static readonly List<string> ServerErrors = new();

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        ServerFixture.InitializeDatabaseOnce();

        var connectionString = ResolveConnectionString();
        EnableSqliteWalMode(connectionString);
        TestHost.GetRequiredService<IDatabaseConfiguration>().ResetConnectionPool();

        await StartMcpHttpServer(connectionString);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (McpClientInstance != null)
        {
            await McpClientInstance.DisposeAsync();
            McpClientInstance = null;
        }

        if (_serverProcess != null && !_serverProcess.HasExited)
        {
            _serverProcess.Kill(entireProcessTree: true);
            _serverProcess.Dispose();
            _serverProcess = null;
        }
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
        var configuration = TestHost.GetRequiredService<IConfiguration>();
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

    private static async Task StartMcpHttpServer(string connectionString)
    {
        var mcpServerProjectDir = Path.GetFullPath(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, McpServerRelativeProjectDir));

        TestContext.Out.WriteLine($"McpHttpServerFixture: building MCP server at {mcpServerProjectDir}");

        var buildProcess = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{mcpServerProjectDir}\" --configuration {BuildConfiguration}",
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
                TestContext.Out.WriteLine($"McpHttpServerFixture: build failed: {stderr}");
                ServerAvailable = false;
                return;
            }
        }

        TestContext.Out.WriteLine("McpHttpServerFixture: build succeeded, starting MCP server in HTTP mode");

        try
        {
            _serverProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --no-build --configuration {BuildConfiguration} --project \"{mcpServerProjectDir}\" -- --http",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Environment =
                {
                    ["ConnectionStrings__SqlConnectionString"] = connectionString,
                    ["ASPNETCORE_URLS"] = ServerUrl
                }
            });

            if (_serverProcess == null)
            {
                TestContext.Out.WriteLine("McpHttpServerFixture: failed to start server process");
                ServerAvailable = false;
                return;
            }

            _serverProcess.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    ServerErrors.Add(e.Data);
                    TestContext.Out.WriteLine($"[McpHttpServer stderr] {e.Data}");
                }
            };
            _serverProcess.BeginErrorReadLine();

            // Wait for the HTTP server to become ready
            await WaitForServerReady();

            var transport = new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = new Uri(ServerUrl),
                Name = "ChurchBulletin-HTTP"
            });

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            McpClientInstance = await McpClient.CreateAsync(transport, cancellationToken: cts.Token);
            Tools = await McpClientInstance.ListToolsAsync(cancellationToken: cts.Token);

            TestContext.Out.WriteLine($"McpHttpServerFixture: connected via HTTP, {Tools.Count} tools discovered");
            ServerAvailable = true;
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"McpHttpServerFixture: failed to start MCP HTTP server: {ex.Message}");
            ServerAvailable = false;
        }
    }

    private static async Task WaitForServerReady()
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await httpClient.GetAsync(ServerUrl);
                // Any response means the server is listening
                TestContext.Out.WriteLine("McpHttpServerFixture: server is accepting connections");
                return;
            }
            catch
            {
                await Task.Delay(500);
            }
        }

        TestContext.Out.WriteLine("McpHttpServerFixture: timed out waiting for server to start");
    }
}
