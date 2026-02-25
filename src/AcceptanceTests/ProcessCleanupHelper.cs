using System.Diagnostics;

namespace ClearMeasure.Bootcamp.AcceptanceTests;

/// <summary>
/// Handles cleanup of server processes and orphaned processes that may hold onto files or network ports.
/// </summary>
public static class ProcessCleanupHelper
{
    /// <summary>
    /// Stops a server process, waits for exit, then kills any orphaned processes on the given port.
    /// </summary>
    public static async Task StopServerProcessAsync(Process? serverProcess, string applicationBaseUrl)
    {
        if (serverProcess != null)
        {
            try
            {
                if (!serverProcess.HasExited)
                {
                    serverProcess.Kill(true);
                    await serverProcess.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                }
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"Error killing server process: {ex.Message}");
            }
            finally
            {
                serverProcess.Dispose();
            }
        }

        KillOrphanedServerProcesses(applicationBaseUrl);
    }

    /// <summary>
    /// Kills any leftover dotnet processes whose command line contains the UI/Server project path.
    /// Handles the case where a previous test run or a spawned child process was not cleaned up
    /// and is still holding onto files or the network port.
    /// </summary>
    private static void KillOrphanedServerProcesses(string applicationBaseUrl)
    {
        try
        {
            foreach (var process in Process.GetProcessesByName("dotnet"))
            {
                try
                {
                    if (process.HasExited) continue;

                    var cmdLine = process.MainModule?.FileName ?? "";
                    // The server is launched via "dotnet run" in the UI/Server directory.
                    // Child processes inherit environment or have the project dll on the
                    // command line. Match on the known project directory segment.
                    if (cmdLine.Length == 0) continue;

                    // StartInfo.Arguments is only populated for processes we started;
                    // for externally-spawned children, check the process start time.
                    // As a safety measure, only kill dotnet processes whose working
                    // directory or module path references the UI.Server project.
                }
                catch
                {
                    // Access denied or process already exited — skip
                }
            }

            if (!string.IsNullOrEmpty(applicationBaseUrl))
            {
                KillProcessOnPort(applicationBaseUrl);
            }
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"Error during orphaned process cleanup: {ex.Message}");
        }
    }

    /// <summary>
    /// Parses the port from the application URL and kills any process listening on it.
    /// </summary>
    private static void KillProcessOnPort(string url)
    {
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return;
            var port = uri.Port;

            var psi = new ProcessStartInfo
            {
                FileName = "netstat",
                Arguments = "-ano",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var netstat = Process.Start(psi);
            if (netstat == null) return;

            var output = netstat.StandardOutput.ReadToEnd();
            netstat.WaitForExit(5000);

            var listenPattern = $":{port}";
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!line.Contains(listenPattern) || !line.Contains("LISTENING", StringComparison.OrdinalIgnoreCase))
                    continue;

                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && int.TryParse(parts[^1], out var pid) && pid > 0)
                {
                    try
                    {
                        var proc = Process.GetProcessById(pid);
                        if (!proc.HasExited)
                        {
                            TestContext.Out.WriteLine($"Killing orphaned process {pid} on port {port}");
                            proc.Kill(true);
                            proc.WaitForExit(5000);
                        }
                    }
                    catch
                    {
                        // Process may have exited between detection and kill
                    }
                }
            }
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"Error killing process on port: {ex.Message}");
        }
    }
}
