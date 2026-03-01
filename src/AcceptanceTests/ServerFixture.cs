using System.Diagnostics;
using System.Net;
using ClearMeasure.Bootcamp.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ClearMeasure.Bootcamp.AcceptanceTests;

[SetUpFixture]
public class ServerFixture
{
    private const string ProjectPath = "../../../../UI/Server";
    private const string WorkerProjectPath = "../../../../Worker";
    private const int WaitTimeoutSeconds = 60;

    private static string BuildConfiguration =>
        AppDomain.CurrentDomain.BaseDirectory.Contains(
            Path.DirectorySeparatorChar + "Release" + Path.DirectorySeparatorChar)
            ? "Release"
            : "Debug";
    public static bool StartLocalServer { get; set; } = true;
    public static int SlowMo { get; set; } = 100;
    public static string ApplicationBaseUrl { get; private set; } = string.Empty;
    private Process? _serverProcess;
    private Process? _workerProcess;
    public static bool StartWorker { get; set; } = true;
    public static bool WorkerStarted { get; private set; }
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
        StartWorker = configuration.GetValue("StartWorker", true);
        SkipScreenshotsForSpeed = configuration.GetValue<bool>("SkipScreenshotsForSpeed");
        SlowMo = configuration.GetValue<int>("SlowMo");
        HeadlessTestBrowser = configuration.GetValue<bool>("HeadlessTestBrowser");


        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        if (StartLocalServer)
        {
            await StartAndWaitForServer();
            await ResetServerDbConnections();
            await StartAndWaitForWorker();
        }

        await WarmUpContainerApp();
        await VerifyApplicationHealthy();
        await new BlazorWasmWarmUp(Playwright, ApplicationBaseUrl).ExecuteAsync();
    }

    /// <summary>
    /// Sends HTTP warm-up requests to the Container App before Playwright browsers launch.
    /// This primes server-side caches, JIT compilation, and triggers Blazor WASM bundle download
    /// so that browser-based tests encounter a warmed-up application.
    /// </summary>
    private static async Task WarmUpContainerApp()
    {
        if (StartLocalServer) return; // local server is already warmed by StartAndWaitForServer

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };

        string[] warmUpPaths = ["/", "/_healthcheck", "/_clienthealthcheck"];

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            TestContext.Out.WriteLine($"HTTP warm-up: round {attempt}/3");
            foreach (var path in warmUpPaths)
            {
                try
                {
                    var response = await client.GetAsync($"{ApplicationBaseUrl}{path}");
                    TestContext.Out.WriteLine($"  {path} -> {(int)response.StatusCode}");
                }
                catch (Exception ex)
                {
                    TestContext.Out.WriteLine($"  {path} -> {ex.GetType().Name}: {ex.Message}");
                }
            }

            await Task.Delay(2000);
        }
    }

    /// <summary>
    /// Verifies the application is reachable and healthy before tests start.
    /// Checks the site root and the /_healthcheck endpoint (which validates database
    /// connectivity). Fails fast with a clear diagnostic message instead of letting
    /// tests hang on an unreachable or unhealthy server.
    /// </summary>
    private static async Task VerifyApplicationHealthy()
    {
        const int maxAttempts = 3;
        const int delayBetweenAttemptsMs = 5000;

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };

        // 1. Verify site is reachable
        TestContext.Out.WriteLine("Health gate: verifying site is reachable...");
        HttpResponseMessage? siteResponse = null;
        Exception? lastSiteException = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                siteResponse = await client.GetAsync(ApplicationBaseUrl);
                TestContext.Out.WriteLine($"  GET {ApplicationBaseUrl} -> {(int)siteResponse.StatusCode}");
                if (siteResponse.IsSuccessStatusCode) break;
            }
            catch (Exception ex)
            {
                lastSiteException = ex;
                TestContext.Out.WriteLine($"  GET {ApplicationBaseUrl} -> {ex.GetType().Name}: {ex.Message}");
            }

            if (attempt < maxAttempts) await Task.Delay(delayBetweenAttemptsMs);
        }

        if (siteResponse == null || !siteResponse.IsSuccessStatusCode)
        {
            var detail = lastSiteException != null
                ? $"Last exception: {lastSiteException.GetType().Name}: {lastSiteException.Message}"
                : $"Last status code: {siteResponse?.StatusCode}";
            Assert.Fail(
                $"Health gate FAILED: Site is not reachable at {ApplicationBaseUrl} after {maxAttempts} attempts. {detail}");
        }

        // 2. Verify /_healthcheck returns Healthy (includes database connectivity)
        TestContext.Out.WriteLine("Health gate: verifying /_healthcheck...");
        var healthUrl = $"{ApplicationBaseUrl}/_healthcheck";
        string? healthBody = null;
        HttpStatusCode? healthStatus = null;
        Exception? lastHealthException = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var response = await client.GetAsync(healthUrl);
                healthStatus = response.StatusCode;
                healthBody = await response.Content.ReadAsStringAsync();
                TestContext.Out.WriteLine($"  GET {healthUrl} -> {(int)response.StatusCode}: {healthBody}");
                if (response.IsSuccessStatusCode && IsAcceptableHealthStatus(healthBody))
                    break;
            }
            catch (Exception ex)
            {
                lastHealthException = ex;
                TestContext.Out.WriteLine($"  GET {healthUrl} -> {ex.GetType().Name}: {ex.Message}");
            }

            if (attempt < maxAttempts) await Task.Delay(delayBetweenAttemptsMs);
        }

        if (healthBody == null || !IsAcceptableHealthStatus(healthBody))
        {
            var detail = lastHealthException != null
                ? $"Last exception: {lastHealthException.GetType().Name}: {lastHealthException.Message}"
                : $"Status: {healthStatus}, Body: {healthBody}";
            Assert.Fail(
                $"Health gate FAILED: /_healthcheck did not return Healthy or Degraded after {maxAttempts} attempts. {detail}");
        }

        TestContext.Out.WriteLine("Health gate: PASSED - site is reachable and healthy.");
    }

    private static bool IsAcceptableHealthStatus(string body) =>
        body.Contains("Healthy", StringComparison.OrdinalIgnoreCase) ||
        body.Contains("Degraded", StringComparison.OrdinalIgnoreCase);

    private async Task StartAndWaitForServer()
    {
        var configuration = TestHost.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("SqlConnectionString") ?? "";
        var useSqlite = connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase);

        // Use --no-build to skip recompilation (already built by the build script)
        // and --no-launch-profile to prevent launchSettings.json from overriding
        // environment variables (e.g. connection strings) set by the test harness
        var config = BuildConfiguration;
        var arguments = useSqlite
            ? $"run --no-build --configuration {config} --no-launch-profile --urls={ApplicationBaseUrl}"
            : $"run --no-build --configuration {config} --urls={ApplicationBaseUrl}";

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

    /// <summary>
    /// Starts the Worker process (NServiceBus message handler host) alongside the UI.Server.
    /// Worker requires SqlServerTransport, so it is skipped when using SQLite.
    /// The Worker's RemotableBus calls back to UI.Server, so UI.Server must be started first.
    /// </summary>
    private async Task StartAndWaitForWorker()
    {
        var configuration = TestHost.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("SqlConnectionString") ?? "";
        var useSqlite = connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase);

        if (!StartWorker)
        {
            TestContext.Out.WriteLine("Worker: skipped (StartWorker=false).");
            return;
        }

        if (useSqlite)
        {
            TestContext.Out.WriteLine("Worker: skipped (SQLite mode — Worker requires SqlServerTransport).");
            return;
        }

        TestContext.Out.WriteLine("Worker: starting...");
        var config = BuildConfiguration;
        var arguments = $"run --no-build --configuration {config} --no-launch-profile";

        _workerProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = arguments,
                WorkingDirectory = WorkerProjectPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        // Connection string for SqlServerTransport
        _workerProcess.StartInfo.Environment["ConnectionStrings__SqlConnectionString"] = connectionString;

        // Point Worker's RemotableBus back to the test UI.Server instance
        _workerProcess.StartInfo.Environment["RemotableBus__ApiUrl"] =
            $"{ApplicationBaseUrl}/api/blazor-wasm-single-api";

        _workerProcess.StartInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";
        _workerProcess.StartInfo.Environment["DISABLE_AUTO_CANCEL_AGENT"] = "true";

        // Provide a dummy Application Insights connection string to prevent the
        // Azure Monitor exporter from throwing when --no-launch-profile is used
        _workerProcess.StartInfo.Environment["APPLICATIONINSIGHTS_CONNECTION_STRING"] =
            "InstrumentationKey=00000000-0000-0000-0000-000000000000";

        // Prevent the Worker from trying to connect to Azure OpenAI
        _workerProcess.StartInfo.Environment["AI_OpenAI_ApiKey"] = "";
        _workerProcess.StartInfo.Environment["AI_OpenAI_Url"] = "";
        _workerProcess.StartInfo.Environment["AI_OpenAI_Model"] = "";

        _workerProcess.Start();

        // Worker is a generic host (no HTTP endpoint to poll). Monitor stdout for the
        // NServiceBus endpoint startup confirmation. NServiceBus logs when the endpoint
        // is successfully started. As a safety net, SqlServerTransport is durable — any
        // messages published before Worker is fully ready will be consumed once it starts.
        var readySignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        _workerProcess.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            TestContext.Out.WriteLine($"  [Worker stdout] {e.Data}");
            // NServiceBus logs the endpoint name when it starts successfully
            if (e.Data.Contains("started", StringComparison.OrdinalIgnoreCase))
            {
                readySignal.TrySetResult(true);
            }
        };

        _workerProcess.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
                TestContext.Out.WriteLine($"  [Worker stderr] {e.Data}");
        };

        _workerProcess.BeginOutputReadLine();
        _workerProcess.BeginErrorReadLine();

        // Wait for the ready signal or timeout
        var timeout = Task.Delay(TimeSpan.FromSeconds(WaitTimeoutSeconds));
        var completed = await Task.WhenAny(readySignal.Task, timeout);

        if (completed == timeout)
        {
            TestContext.Out.WriteLine(
                $"Worker: did not detect startup confirmation within {WaitTimeoutSeconds}s. " +
                "Proceeding anyway — SqlServerTransport is durable and will deliver queued messages.");
        }
        else
        {
            TestContext.Out.WriteLine("Worker: started successfully.");
        }

        WorkerStarted = true;
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

    internal static void InitializeDatabaseOnce()
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
            TestHost.GetRequiredService<IDatabaseConfiguration>().ResetConnectionPool();

            DatabaseInitialized = true;
        }
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        // Stop Worker first (it calls back to UI.Server via RemotableBus)
        await ProcessCleanupHelper.StopProcessAsync(_workerProcess);
        try { _workerProcess?.Dispose(); } catch (ObjectDisposedException) { }
        _workerProcess = null;

        await ProcessCleanupHelper.StopServerProcessAsync(_serverProcess, ApplicationBaseUrl);
        try { _serverProcess?.Dispose(); } catch (ObjectDisposedException) { }
        _serverProcess = null;
        Playwright?.Dispose();
    }
}