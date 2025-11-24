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

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var configuration = TestHost.GetRequiredService<IConfiguration>();
        ApplicationBaseUrl = configuration["ApplicationBaseUrl"] ?? throw new InvalidOperationException();
        StartLocalServer = configuration.GetValue<bool>("StartLocalServer");
        SkipScreenshotsForSpeed = configuration.GetValue<bool>("SkipScreenshotsForSpeed");
        SlowMo = configuration.GetValue<int>("SlowMo");
        if (!StartLocalServer) return;

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

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        if (_serverProcess != null && !_serverProcess.HasExited)
        {
            _serverProcess.Kill(true);
            _serverProcess.Dispose();
        }
    }
}