using System.Diagnostics;
using System.Net;
using ClearMeasure.Bootcamp.Core;
using Microsoft.EntityFrameworkCore;

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
    private static readonly Lock DatabaseLock = new();
    
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

        // Playwright's Expect(...) assertions do NOT inherit the browser context's
        // default timeout (set to 60s in AcceptanceTestBase) — they default to 5s.
        // Under LevelOfParallelism(4) a cold Blazor WASM render can exceed 5s, so an
        // assertion that is merely waiting for the first render fails with
        // "<element(s) not found>". Align the assertion budget with the action budget.
        Assertions.SetDefaultExpectTimeout(30_000);

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

        var client = TestHttpClientFactory.CreateInsecureClient();

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
        var client = TestHttpClientFactory.CreateInsecureClient();

        TestContext.Out.WriteLine("Health gate: verifying site is reachable...");
        var siteResult = await TryReachSite(client, maxAttempts, delayBetweenAttemptsMs);
        if (!siteResult.Succeeded)
        {
            Assert.Fail(
                $"Health gate FAILED: Site is not reachable at {ApplicationBaseUrl} after {maxAttempts} attempts. {siteResult.Detail}");
        }

        TestContext.Out.WriteLine("Health gate: verifying /_healthcheck...");
        var healthResult = await TryGetHealthBody(client, maxAttempts, delayBetweenAttemptsMs);
        if (!healthResult.Succeeded)
        {
            Assert.Fail(
                $"Health gate FAILED: /_healthcheck did not return Healthy or Degraded after {maxAttempts} attempts. {healthResult.Detail}");
        }

        TestContext.Out.WriteLine("Health gate: PASSED - site is reachable and healthy.");
    }

    private static async Task<(bool Succeeded, string Detail)> TryReachSite(
        HttpClient client,
        int maxAttempts,
        int delayBetweenAttemptsMs)
    {
        HttpResponseMessage? siteResponse = null;
        Exception? lastSiteException = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            (siteResponse, lastSiteException) =
                await AttemptGet(client, ApplicationBaseUrl, siteResponse);
            if (siteResponse?.IsSuccessStatusCode == true)
            {
                return (true, string.Empty);
            }

            await DelayIfMoreAttempts(attempt, maxAttempts, delayBetweenAttemptsMs);
        }

        return (false, FormatReachFailure(lastSiteException, siteResponse));
    }

    private static async Task<(bool Succeeded, string Detail)> TryGetHealthBody(
        HttpClient client,
        int maxAttempts,
        int delayBetweenAttemptsMs)
    {
        var healthUrl = $"{ApplicationBaseUrl}/_healthcheck";
        string? healthBody = null;
        HttpStatusCode? healthStatus = null;
        Exception? lastHealthException = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var attemptResult = await AttemptHealthGet(client, healthUrl);
            if (attemptResult.Exception != null)
            {
                lastHealthException = attemptResult.Exception;
            }
            else
            {
                lastHealthException = null;
                healthStatus = attemptResult.Status;
                healthBody = attemptResult.Body;
                if (IsSuccessfulHealth(attemptResult.Status, attemptResult.Body))
                {
                    return (true, string.Empty);
                }
            }

            await DelayIfMoreAttempts(attempt, maxAttempts, delayBetweenAttemptsMs);
        }

        return (false, FormatHealthFailure(lastHealthException, healthStatus, healthBody));
    }

    private static async Task<(HttpResponseMessage? Response, Exception? Exception)> AttemptGet(
        HttpClient client,
        string url,
        HttpResponseMessage? previousResponse)
    {
        try
        {
            var response = await client.GetAsync(url);
            TestContext.Out.WriteLine($"  GET {url} -> {(int)response.StatusCode}");
            return (response, null);
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"  GET {url} -> {ex.GetType().Name}: {ex.Message}");
            return (previousResponse, ex);
        }
    }

    private static async Task<(HttpStatusCode? Status, string? Body, Exception? Exception)> AttemptHealthGet(
        HttpClient client,
        string healthUrl)
    {
        try
        {
            var response = await client.GetAsync(healthUrl);
            var body = await response.Content.ReadAsStringAsync();
            TestContext.Out.WriteLine($"  GET {healthUrl} -> {(int)response.StatusCode}: {body}");
            return (response.StatusCode, body, null);
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"  GET {healthUrl} -> {ex.GetType().Name}: {ex.Message}");
            return (null, null, ex);
        }
    }

    private static bool IsSuccessfulHealth(HttpStatusCode? status, string? body)
    {
        if (status is null || body is null)
        {
            return false;
        }

        var code = (int)status.Value;
        return code is >= 200 and < 300 && IsAcceptableHealthStatus(body);
    }

    private static async Task DelayIfMoreAttempts(int attempt, int maxAttempts, int delayBetweenAttemptsMs)
    {
        if (attempt < maxAttempts)
        {
            await Task.Delay(delayBetweenAttemptsMs);
        }
    }

    private static string FormatReachFailure(Exception? lastSiteException, HttpResponseMessage? siteResponse) =>
        lastSiteException != null
            ? $"Last exception: {lastSiteException.GetType().Name}: {lastSiteException.Message}"
            : $"Last status code: {siteResponse?.StatusCode}";

    private static string FormatHealthFailure(
        Exception? lastHealthException,
        HttpStatusCode? healthStatus,
        string? healthBody) =>
        lastHealthException != null
            ? $"Last exception: {lastHealthException.GetType().Name}: {lastHealthException.Message}"
            : $"Status: {healthStatus}, Body: {healthBody}";

    private static bool IsAcceptableHealthStatus(string body) =>
        body.Contains("Healthy", StringComparison.OrdinalIgnoreCase) ||
        body.Contains("Degraded", StringComparison.OrdinalIgnoreCase);

    private async Task StartAndWaitForServer()
    {
        var connectionString = GetSqlConnectionString();
        var useSqlite = IsSqliteConnection(connectionString);
        _serverProcess = CreateDotnetProcess(ProjectPath, BuildServerArguments(useSqlite));
        ConfigureServerEnvironment(_serverProcess, useSqlite, connectionString);
        AttachProcessLogging(_serverProcess, "Server");
        _serverProcess.Start();
        _serverProcess.BeginOutputReadLine();
        _serverProcess.BeginErrorReadLine();
        await WaitUntilUrlReady(ApplicationBaseUrl);
    }

    private static string BuildServerArguments(bool useSqlite)
    {
        var config = BuildConfiguration;
        return useSqlite
            ? $"run --no-build --configuration {config} --no-launch-profile --urls={ApplicationBaseUrl}"
            : $"run --no-build --configuration {config} --urls={ApplicationBaseUrl}";
    }

    private static void ConfigureServerEnvironment(Process process, bool useSqlite, string connectionString)
    {
        process.StartInfo.Environment["DISABLE_AUTO_CANCEL_AGENT"] = "true";
        process.StartInfo.Environment["ApiKeyAuthentication__Enabled"] = "false";
        process.StartInfo.Environment["ApiKeyAuthentication__ValidationKey"] = "";
        if (useSqlite)
        {
            ApplySqliteServerEnvironment(process, connectionString);
        }
    }

    private static void ApplySqliteServerEnvironment(Process process, string connectionString)
    {
        process.StartInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        process.StartInfo.Environment["APPLICATIONINSIGHTS_CONNECTION_STRING"] =
            "InstrumentationKey=00000000-0000-0000-0000-000000000000";
        process.StartInfo.Environment["ConnectionStrings__SqlConnectionString"] =
            ResolveSqliteConnectionString(connectionString);
    }

    private static string ResolveSqliteConnectionString(string connectionString)
    {
        if (connectionString.Contains(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var dbPath = connectionString["Data Source=".Length..].Trim();
        var semicolonIndex = dbPath.IndexOf(';');
        if (semicolonIndex >= 0)
        {
            dbPath = dbPath[..semicolonIndex];
        }

        if (!Path.IsPathRooted(dbPath))
        {
            dbPath = Path.GetFullPath(dbPath);
        }

        return $"Data Source={dbPath}";
    }

    private static async Task WaitUntilUrlReady(string baseUrl)
    {
        var client = TestHttpClientFactory.CreateInsecureClient();
        var timeout = TimeSpan.FromSeconds(WaitTimeoutSeconds);
        var start = DateTime.UtcNow;
        Exception? lastException = null;
        while (DateTime.UtcNow - start < timeout)
        {
            try
            {
                var response = await client.GetAsync(baseUrl);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
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
        var connectionString = GetSqlConnectionString();
        if (ShouldSkipWorker(IsSqliteConnection(connectionString)))
        {
            return;
        }

        TestContext.Out.WriteLine("Worker: starting...");
        var config = BuildConfiguration;
        _workerProcess = CreateDotnetProcess(
            WorkerProjectPath,
            $"run --no-build --configuration {config} --no-launch-profile");
        ConfigureWorkerEnvironment(_workerProcess, connectionString);
        var readySignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        AttachWorkerLogging(_workerProcess, readySignal);
        _workerProcess.Start();
        _workerProcess.BeginOutputReadLine();
        _workerProcess.BeginErrorReadLine();
        await WaitForWorkerReadySignal(readySignal);
        WorkerStarted = true;
    }

    private static bool ShouldSkipWorker(bool useSqlite)
    {
        if (!StartWorker)
        {
            TestContext.Out.WriteLine("Worker: skipped (StartWorker=false).");
            return true;
        }

        if (useSqlite)
        {
            TestContext.Out.WriteLine("Worker: skipped (SQLite mode — Worker requires SqlServerTransport).");
            return true;
        }

        return false;
    }

    private static void ConfigureWorkerEnvironment(Process process, string connectionString)
    {
        process.StartInfo.Environment["ConnectionStrings__SqlConnectionString"] = connectionString;
        process.StartInfo.Environment["RemotableBus__ApiUrl"] =
            $"{ApplicationBaseUrl}/api/blazor-wasm-single-api";
        process.StartInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";
        process.StartInfo.Environment["DISABLE_AUTO_CANCEL_AGENT"] = "true";
        process.StartInfo.Environment["APPLICATIONINSIGHTS_CONNECTION_STRING"] =
            "InstrumentationKey=00000000-0000-0000-0000-000000000000";
        process.StartInfo.Environment["AI_OpenAI_ApiKey"] = "";
        process.StartInfo.Environment["AI_OpenAI_Url"] = "";
        process.StartInfo.Environment["AI_OpenAI_Model"] = "";
    }

    private static void AttachWorkerLogging(Process process, TaskCompletionSource<bool> readySignal)
    {
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null)
            {
                return;
            }

            TestContext.Out.WriteLine($"  [Worker stdout] {e.Data}");
            if (e.Data.Contains("started", StringComparison.OrdinalIgnoreCase))
            {
                readySignal.TrySetResult(true);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                TestContext.Out.WriteLine($"  [Worker stderr] {e.Data}");
            }
        };
    }

    private static async Task WaitForWorkerReadySignal(TaskCompletionSource<bool> readySignal)
    {
        var timeout = Task.Delay(TimeSpan.FromSeconds(WaitTimeoutSeconds));
        var completed = await Task.WhenAny(readySignal.Task, timeout);
        if (completed == timeout)
        {
            TestContext.Out.WriteLine(
                $"Worker: did not detect startup confirmation within {WaitTimeoutSeconds}s. " +
                "Proceeding anyway — SqlServerTransport is durable and will deliver queued messages.");
            return;
        }

        TestContext.Out.WriteLine("Worker: started successfully.");
    }

    private static string GetSqlConnectionString()
    {
        var configuration = TestHost.GetRequiredService<IConfiguration>();
        return configuration.GetConnectionString("SqlConnectionString") ?? "";
    }

    private static bool IsSqliteConnection(string connectionString) =>
        connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase);

    private static Process CreateDotnetProcess(string workingDirectory, string arguments) =>
        new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

    private static void AttachProcessLogging(Process process, string label)
    {
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                TestContext.Out.WriteLine($"  [{label} stdout] {e.Data}");
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                TestContext.Out.WriteLine($"  [{label} stderr] {e.Data}");
            }
        };
    }

    private static async Task ResetServerDbConnections()
    {
        var client = TestHttpClientFactory.CreateInsecureClient();
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