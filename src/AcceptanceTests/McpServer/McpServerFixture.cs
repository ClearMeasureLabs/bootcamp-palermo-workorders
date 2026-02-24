using System.Diagnostics;
using ClearMeasure.Bootcamp.AcceptanceTests;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.IntegrationTests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;

namespace ClearMeasure.Bootcamp.McpAcceptanceTests;

[SetUpFixture]
public class McpServerFixture
{
    private const string McpServerRelativeProjectDir = "../../../../McpServer";

    private static string BuildConfiguration =>
        AppDomain.CurrentDomain.BaseDirectory.Contains(
            Path.DirectorySeparatorChar + "Release" + Path.DirectorySeparatorChar)
            ? "Release"
            : "Debug";

    public static McpClient? StdioMcpClientInstance { get; private set; }
    public static IList<McpClientTool>? StdioTools { get; private set; }
    public static bool StdioServerAvailable { get; private set; }

    public static McpClient? HttpMcpClientInstance { get; private set; }
    public static IList<McpClientTool>? HttpTools { get; private set; }
    public static bool HttpServerAvailable { get; private set; }

    public static bool LlmAvailable { get; private set; }
    public static string LlmProvider { get; private set; } = "None";
    private static readonly List<string> ServerErrors = new();

    // Backward-compatible aliases for existing code
    public static McpClient? McpClientInstance => StdioMcpClientInstance;
    public static IList<McpClientTool>? Tools => StdioTools;
    public static bool ServerAvailable => StdioServerAvailable;

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

        await StartStdioMcpServer(connectionString);
        await ConnectHttpMcpClient();
        await CheckLlmAvailability();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (StdioMcpClientInstance != null)
        {
            await StdioMcpClientInstance.DisposeAsync();
            StdioMcpClientInstance = null;
        }

        if (HttpMcpClientInstance != null)
        {
            await HttpMcpClientInstance.DisposeAsync();
            HttpMcpClientInstance = null;
        }
    }

    private static void EnsureDatabaseInitialized()
    {
        ServerFixture.InitializeDatabaseOnce();
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

    private static async Task StartStdioMcpServer(string connectionString)
    {
        var mcpServerProjectDir = Path.GetFullPath(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, McpServerRelativeProjectDir));

        TestContext.Out.WriteLine($"McpServerFixture: building MCP server at {mcpServerProjectDir}");

        // Pre-build so the stdio connection doesn't time out waiting for compilation
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
                TestContext.Out.WriteLine($"McpServerFixture: build failed: {stderr}");
                StdioServerAvailable = false;
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
                Arguments = ["run", "--no-build", "--configuration", BuildConfiguration, "--project", mcpServerProjectDir],
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
            StdioMcpClientInstance = await McpClient.CreateAsync(transport, cancellationToken: cts.Token);
            StdioTools = await StdioMcpClientInstance.ListToolsAsync(cancellationToken: cts.Token);

            TestContext.Out.WriteLine($"McpServerFixture: stdio connected, {StdioTools.Count} tools discovered");
            StdioServerAvailable = true;
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"McpServerFixture: failed to start stdio MCP server: {ex.Message}");
            StdioServerAvailable = false;
        }
    }

    private static async Task ConnectHttpMcpClient()
    {
        var baseUrl = ServerFixture.ApplicationBaseUrl;
        if (string.IsNullOrEmpty(baseUrl))
        {
            var configuration = TestHost.GetRequiredService<IConfiguration>();
            baseUrl = configuration["ApplicationBaseUrl"] ?? "";
        }

        if (string.IsNullOrEmpty(baseUrl))
        {
            TestContext.Out.WriteLine("McpServerFixture: no ApplicationBaseUrl configured, HTTP MCP transport unavailable");
            HttpServerAvailable = false;
            return;
        }

        try
        {
            var endpoint = new Uri($"{baseUrl}/mcp");
            TestContext.Out.WriteLine($"McpServerFixture: connecting to HTTP MCP endpoint at {endpoint}");

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            var httpClient = new HttpClient(handler);

            var options = new HttpClientTransportOptions
            {
                Endpoint = endpoint,
                Name = "ChurchBulletin-HTTP"
            };

            var transport = new HttpClientTransport(options, httpClient,
                NullLoggerFactory.Instance, ownsHttpClient: true);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            HttpMcpClientInstance = await McpClient.CreateAsync(transport, cancellationToken: cts.Token);
            HttpTools = await HttpMcpClientInstance.ListToolsAsync(cancellationToken: cts.Token);

            TestContext.Out.WriteLine($"McpServerFixture: HTTP connected, {HttpTools.Count} tools discovered");
            HttpServerAvailable = true;
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"McpServerFixture: HTTP MCP connection failed: {ex.Message}");
            HttpServerAvailable = false;
        }
    }

    internal static string? GetLlmConfigValue(string key)
    {
        // Check IConfiguration first (includes user secrets), then fall back to env vars
        var configuration = TestHost.GetRequiredService<IConfiguration>();
        var value = configuration.GetValue<string>(key);
        if (!string.IsNullOrEmpty(value)) return value;

        return Environment.GetEnvironmentVariable(key);
    }

    private static async Task CheckLlmAvailability()
    {
        // Azure OpenAI takes priority when configured
        var apiKey = GetLlmConfigValue("AI_OpenAI_ApiKey");
        if (!string.IsNullOrEmpty(apiKey))
        {
            var url = GetLlmConfigValue("AI_OpenAI_Url");
            var model = GetLlmConfigValue("AI_OpenAI_Model");
            if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(model))
            {
                LlmAvailable = true;
                LlmProvider = "AzureOpenAI";
                TestContext.Out.WriteLine($"McpServerFixture: Azure OpenAI configured (model={model})");
                return;
            }
        }

        // Fall back to local Ollama
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await client.GetAsync("http://localhost:11434/");
            LlmAvailable = response.IsSuccessStatusCode;
            LlmProvider = LlmAvailable ? "Ollama" : "None";
            TestContext.Out.WriteLine($"McpServerFixture: Ollama available = {LlmAvailable}");
        }
        catch
        {
            LlmAvailable = false;
            LlmProvider = "None";
            TestContext.Out.WriteLine("McpServerFixture: Ollama not available");
        }
    }
}
