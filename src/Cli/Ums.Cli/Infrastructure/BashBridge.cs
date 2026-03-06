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
            FileName               = "/bin/bash",
            Arguments              = $"-c {bashArgs}",
            UseShellExecute        = false,
            RedirectStandardOutput = false,
            RedirectStandardError  = false,
            RedirectStandardInput  = false,
        };
        using var proc = Process.Start(psi)!;
        proc.WaitForExit();
        if (proc.ExitCode != 0) Environment.Exit(proc.ExitCode);
    }

    public static void RunRaw(string command)
    {
        var psi = new ProcessStartInfo
        {
            FileName               = "/bin/bash",
            Arguments              = $"-c \"{command.Replace("\"", "\\\"")}\"",
            UseShellExecute        = false,
            RedirectStandardOutput = false,
            RedirectStandardError  = false,
        };
        using var proc = Process.Start(psi)!;
        proc.WaitForExit();
        if (proc.ExitCode != 0) Environment.Exit(proc.ExitCode);
    }

    public static async Task<string> CaptureAsync(string command, int timeoutMs = 10_000)
    {
        var psi = new ProcessStartInfo
        {
            FileName               = "/bin/bash",
            Arguments              = $"-c \"{command.Replace("\"", "\\\"")}\"",
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
        };
        using var proc = Process.Start(psi)!;
        var stdout = await proc.StandardOutput.ReadToEndAsync();
        var stderr = await proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();
        return proc.ExitCode == 0 ? stdout.Trim() : $"ERROR({proc.ExitCode}): {stderr.Trim()}";
    }

    public static Task<string> CaptureAsync(string command, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return CaptureAsync(command, timeoutMs: 10_000);
    }

    public static async Task RunAsync(string command, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName               = "/bin/bash",
            Arguments              = $"-c \"{command.Replace("\"", "\\\"")}\"",
            UseShellExecute        = false,
            RedirectStandardOutput = false,
            RedirectStandardError  = false,
        };
        using var proc = Process.Start(psi)!;
        await proc.WaitForExitAsync(ct);
    }
}
