using System.Diagnostics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ClearMeasure.Bootcamp.AcceptanceTests;

[SetUpFixture]
public class ServerFixture
{
    private const string ProjectPath = "../../../../UI/Server";
    private const int WaitTimeoutSeconds = 60;
    public static bool StartLocalServer { get; set; } = true;
    public static int SlowMo { get; set; } = 100;
    public static string ApplicationBaseUrl { get; private set; } = string.Empty;
    private Process? _serverProcess;
    public static bool SkipScreenshotsForSpeed { get; set; } = true;
    public static bool HeadlessTestBrowser { get; set; } = true;
    public static bool DatabaseInitialized { get; private set; }
    private static readonly object DatabaseLock = new();
    
    /// <summary>
    /// Shared Playwright instance for all tests. Thread-safe for parallel execution.
    /// </summary>
    public static IPlaywright Playwright { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        InitializeDatabaseOnce();
        var configuration = TestHost.GetRequiredService<IConfiguration>();
        ApplicationBaseUrl = configuration["ApplicationBaseUrl"] ?? throw new InvalidOperationException();
        StartLocalServer = configuration.GetValue<bool>("StartLocalServer");
        SkipScreenshotsForSpeed = configuration.GetValue<bool>("SkipScreenshotsForSpeed");
        SlowMo = configuration.GetValue<int>("SlowMo");
        HeadlessTestBrowser = configuration.GetValue<bool>("HeadlessTestBrowser");


        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        if (StartLocalServer)
        {
            await StartAndWaitForServer();
            await ResetServerDbConnections();
        }

        await new BlazorWasmWarmUp(Playwright, ApplicationBaseUrl).ExecuteAsync();
    }

    private async Task StartAndWaitForServer()
    {
        var configuration = TestHost.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("SqlConnectionString") ?? "";
        var useSqlite = connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase);

        // Use --no-launch-profile to prevent launchSettings.json from overriding
        // environment variables (e.g. connection strings) set by the test harness
        var arguments = useSqlite
            ? $"run --no-launch-profile --urls={ApplicationBaseUrl}"
            : $"run --urls={ApplicationBaseUrl}";

        _serverProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = arguments,
                WorkingDirectory = ProjectPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        _serverProcess.StartInfo.Environment["DISABLE_AUTO_CANCEL_AGENT"] = "true";

        if (useSqlite)
        {
            _serverProcess.StartInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";

            // Provide a dummy Application Insights connection string to prevent the
            // Azure Monitor exporter from throwing when --no-launch-profile is used
            _serverProcess.StartInfo.Environment["APPLICATIONINSIGHTS_CONNECTION_STRING"] =
                "InstrumentationKey=00000000-0000-0000-0000-000000000000";

            // For SQLite file-based databases, resolve to absolute path so the server
            // process uses the same database file regardless of its working directory
            if (!connectionString.Contains(":memory:", StringComparison.OrdinalIgnoreCase))
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

            _serverProcess.StartInfo.Environment["ConnectionStrings__SqlConnectionString"] = connectionString;
        }

        _serverProcess.Start();

        // Wait for server to be ready
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var client = new HttpClient(handler);
        var baseUrl = ApplicationBaseUrl;
        var timeout = TimeSpan.FromSeconds(WaitTimeoutSeconds);
        var start = DateTime.UtcNow;
        Exception? lastException = null;
        while (DateTime.UtcNow - start < timeout)
        {
            try
            {
                var response = await client.GetAsync(baseUrl);
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            await Task.Delay(1000);
        }

        throw new Exception(
            $"UI.Server did not start in {WaitTimeoutSeconds} seconds. Last exception: {lastException}");
    }

    private static async Task ResetServerDbConnections()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var client = new HttpClient(handler);
        var response = await client.PostAsync($"{ApplicationBaseUrl}/_diagnostics/reset-db-connections", null);
        response.EnsureSuccessStatusCode();
    }

    private static void InitializeDatabaseOnce()
    {
        if (DatabaseInitialized) return;

        lock (DatabaseLock)
        {
            if (DatabaseInitialized) return;

            using var context = TestHost.GetRequiredService<DbContext>();
            var isSqlite = context.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
            if (isSqlite)
            {
                context.Database.EnsureCreated();
            }

            new ZDataLoader().LoadData();
            TestContext.Out.WriteLine("ZDataLoader().LoadData(); - complete");

            // Release all pooled connections so the server process opens the
            // database file with a clean view of the seeded data
            if (isSqlite)
            {
                ClearLocalConnectionPools();
            }

            DatabaseInitialized = true;
        }
    }

    private static void ClearLocalConnectionPools()
    {
        var connectionType = Type.GetType(
            "Microsoft.Data.Sqlite.SqliteConnection, Microsoft.Data.Sqlite");
        var clearAllPools = connectionType?.GetMethod("ClearAllPools",
            BindingFlags.Static | BindingFlags.Public);
        clearAllPools?.Invoke(null, null);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_serverProcess != null && !_serverProcess.HasExited)
        {
            _serverProcess.Kill(true);
            _serverProcess.Dispose();
        }
        
        Playwright?.Dispose();
    }
}