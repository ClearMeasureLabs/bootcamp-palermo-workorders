using System.Diagnostics;

namespace ClearMeasure.Bootcamp.AcceptanceTests;

/// <summary>
/// Handles cleanup of server processes and orphaned processes that may hold onto files or network ports.
/// </summary>
public static class ProcessCleanupHelper
{
    /// <summary>
    /// Stops a non-HTTP process (e.g. Worker) by killing its process tree and waiting for exit.
    /// Unlike <see cref="StopServerProcessAsync"/>, no port-based orphan cleanup is performed
    /// because the process does not listen on a network port.
    /// </summary>
    public static async Task StopProcessAsync(Process? process)
    {
        if (process == null) return;

        try
        {
            if (!process.HasExited)
            {
                TestContext.Out.WriteLine($"Stopping process {process.Id}...");
                process.Kill(true);
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            }
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"Error killing process: {ex.Message}");
        }
        finally
        {
            process.Dispose();
        }
    }

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
    /// Windows uses netstat; Linux uses lsof.
    /// </summary>
    private static void KillProcessOnPort(string url)
    {
        try
        {
            if (!TryGetPort(url, out var port))
            {
                return;
            }

            KillProcessOnPortForCurrentOs(port);
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"Error killing process on port: {ex.Message}");
        }
    }

    private static bool TryGetPort(string url, out int port)
    {
        port = 0;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        port = uri.Port;
        return true;
    }

    private static void KillProcessOnPortForCurrentOs(int port)
    {
        if (OperatingSystem.IsWindows())
        {
            KillProcessOnPortWindows(port);
            return;
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            KillProcessOnPortUnix(port);
        }
    }

    private static void KillProcessOnPortWindows(int port)
    {
        var output = RunCapture("netstat", "-ano");
        if (output == null)
        {
            return;
        }

        var listenPattern = $":{port}";
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (TryParseWindowsListeningPid(line, listenPattern, out var pid))
            {
                KillProcessById(pid, port);
            }
        }
    }

    private static bool TryParseWindowsListeningPid(string line, string listenPattern, out int pid)
    {
        pid = 0;
        if (!IsWindowsListeningLine(line, listenPattern))
        {
            return false;
        }

        return TryParseTrailingPid(line, out pid);
    }

    private static bool IsWindowsListeningLine(string line, string listenPattern)
    {
        return line.Contains(listenPattern)
               && line.Contains("LISTENING", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseTrailingPid(string line, out int pid)
    {
        pid = 0;
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        return int.TryParse(parts[^1], out pid) && pid > 0;
    }

    private static void KillProcessOnPortUnix(int port)
    {
        var output = RunCapture("lsof", $"-ti :{port}");
        if (output == null)
        {
            return;
        }

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(line.Trim(), out var pid) && pid > 0)
            {
                KillProcessById(pid, port);
            }
        }
    }

    private static string? RunCapture(string fileName, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = Process.Start(psi);
        if (process == null)
        {
            return null;
        }

        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit(5000);
        return output;
    }

    private static void KillProcessById(int pid, int port)
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
