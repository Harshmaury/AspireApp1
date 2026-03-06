namespace Ums.Cli.Infrastructure;
using System.Diagnostics;

public static class BashBridge
{
    private static readonly string EngineDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "ums", "engine");

    public static void Run(string script, string args)
    {
        var scriptPath = Path.Combine(EngineDir, script);
        if (!File.Exists(scriptPath))
        {
            Console.Error.WriteLine($"[UMS] Engine script not found: {scriptPath}");
            Environment.Exit(1);
        }
        var bashArgs = string.IsNullOrWhiteSpace(args)
            ? $"\"{scriptPath}\""
            : $"\"{scriptPath}\" {args}";
        var psi = new ProcessStartInfo
        {
            FileName = "/bin/bash", Arguments = $"-c {bashArgs}",
            UseShellExecute = false,
        };
        using var proc = Process.Start(psi)!;
        proc.WaitForExit();
        if (proc.ExitCode != 0) Environment.Exit(proc.ExitCode);
    }

    public static void RunRaw(string command)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = $"-c \"{command.Replace("\"", "\\\"")}\"",
            UseShellExecute = false,
        };
        using var proc = Process.Start(psi)!;
        proc.WaitForExit();
        if (proc.ExitCode != 0) Environment.Exit(proc.ExitCode);
    }

    public static async Task<string> CaptureAsync(string command, int timeoutMs = 10_000)
    {
        using var cts = new CancellationTokenSource(timeoutMs);
        return await CaptureAsync(command, cts.Token);
    }

    // FIX: properly propagates CancellationToken to both process kill and stream read
    public static async Task<string> CaptureAsync(string command, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var psi = new ProcessStartInfo
        {
            FileName               = "/bin/bash",
            Arguments              = $"-c \"{command.Replace("\"", "\\\"")}\"",
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
        };

        using var proc = Process.Start(psi)!;

        // Register cancellation to kill the process
        await using var reg = ct.Register(() =>
        {
            try { if (!proc.HasExited) proc.Kill(entireProcessTree: true); }
            catch { /* ignore race */ }
        });

        var stdout = await proc.StandardOutput.ReadToEndAsync(ct);
        var stderr = await proc.StandardError.ReadToEndAsync(ct);
        await proc.WaitForExitAsync(ct);

        return proc.ExitCode == 0 ? stdout.Trim() : $"ERROR({proc.ExitCode}): {stderr.Trim()}";
    }

    public static async Task RunAsync(string command, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName               = "/bin/bash",
            Arguments              = $"-c \"{command.Replace("\"", "\\\"")}\"",
            UseShellExecute        = false,
        };
        using var proc = Process.Start(psi)!;
        await using var reg = ct.Register(() =>
        {
            try { if (!proc.HasExited) proc.Kill(entireProcessTree: true); }
            catch { /* ignore race */ }
        });
        await proc.WaitForExitAsync(ct);
    }
}



