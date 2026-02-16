using System.Diagnostics;
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
    public static bool ReloadTestData { get; set; } = true;
    public static bool DatabaseInitialized { get; private set; }
    private static readonly object DatabaseLock = new();
    
    /// <summary>
    /// Shared Playwright instance for all tests. Thread-safe for parallel execution.
    /// </summary>
    public static IPlaywright Playwright { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var configuration = TestHost.GetRequiredService<IConfiguration>();
        var baseUrlParameter = TestContext.Parameters.Get("ApplicationBaseUrl");
        var startLocalServerParameter = TestContext.Parameters.Get("StartLocalServer");
        var reloadTestDataParameter = TestContext.Parameters.Get("ReloadTestData");

        ApplicationBaseUrl = !string.IsNullOrWhiteSpace(baseUrlParameter)
            ? baseUrlParameter
            : configuration["ApplicationBaseUrl"] ?? throw new InvalidOperationException();

        StartLocalServer = bool.TryParse(startLocalServerParameter, out var startLocalServer)
            ? startLocalServer
            : configuration.GetValue<bool>("StartLocalServer");

        ReloadTestData = bool.TryParse(reloadTestDataParameter, out var reloadData)
            ? reloadData
            : configuration.GetValue<bool>("ReloadTestData");

        SkipScreenshotsForSpeed = configuration.GetValue<bool>("SkipScreenshotsForSpeed");
        SlowMo = configuration.GetValue<int>("SlowMo");
        HeadlessTestBrowser = configuration.GetValue<bool>("HeadlessTestBrowser");

        InitializeDatabaseOnce();

        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        if (StartLocalServer)
        {
            await StartAndWaitForServer();
        }

        await new BlazorWasmWarmUp(Playwright, ApplicationBaseUrl).ExecuteAsync();
    }

    private async Task StartAndWaitForServer()
    {
        _serverProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --urls={ApplicationBaseUrl}",
                WorkingDirectory = ProjectPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        _serverProcess.StartInfo.Environment["DISABLE_AUTO_CANCEL_AGENT"] = "true";
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

    private static void InitializeDatabaseOnce()
    {
        if (!ReloadTestData)
        {
            DatabaseInitialized = true;
            return;
        }

        if (DatabaseInitialized) return;

        lock (DatabaseLock)
        {
            if (DatabaseInitialized) return;

            new ZDataLoader().LoadData();
            TestContext.Out.WriteLine("ZDataLoader().LoadData(); - complete");
            DatabaseInitialized = true;
        }
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