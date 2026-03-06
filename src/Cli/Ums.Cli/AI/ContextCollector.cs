using System.Text;                    // FIX: StringBuilder
using Ums.Cli.Infrastructure;         // FIX: BashBridge

namespace Ums.Cli.AI;

public sealed class ContextCollector
{
    const string Ns      = "ums";
    const int    LogTail = 60;

    static readonly string SnapDir = Path.Combine(
        Environment.GetEnvironmentVariable("UMS_PROJECT_ROOT")
            ?? "/mnt/c/Users/harsh/source/repos/AspireApp1",
        "src", ".ums", "snapshots");

    static readonly Dictionary<string, string> PodLabels =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["identity"]     = "identity-api",
            ["academic"]     = "academic-api",
            ["student"]      = "student-api",
            ["attendance"]   = "attendance-api",
            ["examination"]  = "examination-api",
            ["exam"]         = "examination-api",
            ["fee"]          = "fee-api",
            ["faculty"]      = "faculty-api",
            ["hostel"]       = "hostel-api",
            ["notification"] = "notification-api",
            ["gateway"]      = "api-gateway",
            ["bff"]          = "bff",
            ["kafka"]        = "kafka",
            ["postgres"]     = "postgres",
            ["zookeeper"]    = "zookeeper",
        };

    public async Task<string> CollectAsync(
        string? focusService = null,
        CancellationToken ct = default)
    {
        var sb = new StringBuilder();

        sb.AppendLine("=== POD STATUS ===");
        sb.AppendLine(await Cap($"kubectl get pods -n {Ns} -o wide --no-headers 2>&1", ct));

        sb.AppendLine("\n=== RECENT WARNING EVENTS ===");
        sb.AppendLine(await Cap(
            $"kubectl get events -n {Ns} --field-selector type=Warning " +
            $"--sort-by='.lastTimestamp' 2>&1 | tail -20", ct));

        if (focusService != null && PodLabels.TryGetValue(focusService, out var lbl))
        {
            sb.AppendLine($"\n=== {lbl.ToUpper()} LOGS (last {LogTail} lines) ===");
            sb.AppendLine(await Cap(
                $"kubectl logs -n {Ns} -l app={lbl} --tail={LogTail} " +
                $"--all-containers --prefix 2>&1", ct));

            sb.AppendLine($"\n=== {lbl.ToUpper()} PREVIOUS CONTAINER LOGS ===");
            sb.AppendLine(await Cap(
                $"kubectl logs -n {Ns} -l app={lbl} --tail=30 " +
                $"--previous --all-containers 2>&1", ct));
        }
        else
        {
            sb.AppendLine("\n=== RESTARTED POD LOGS ===");
            var restarted = await Cap(
                $"kubectl get pods -n {Ns} --no-headers 2>&1 | awk '$4 > 0 {{print $1}}' | head -4", ct);

            foreach (var pod in restarted.Split('\n',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                sb.AppendLine($"--- {pod} (restarted) ---");
                sb.AppendLine(await Cap(
                    $"kubectl logs -n {Ns} {pod} --tail=30 --previous 2>&1", ct));
            }
        }

        sb.AppendLine("\n=== KAFKA CONSUMER LAG ===");
        sb.AppendLine(await Cap(
            $"kubectl exec -n {Ns} deploy/kafka -- " +
            $"kafka-consumer-groups.sh --bootstrap-server localhost:9092 " +
            $"--describe --all-groups 2>&1 | head -50", ct));

        sb.AppendLine("\n=== POSTGRES REPLICATION LAG ===");
        sb.AppendLine(await Cap(
            $"kubectl exec -n {Ns} statefulset/postgres -- " +
            $"psql -U postgres -c \"SELECT client_addr, state, sent_lsn, write_lsn, " +
            $"replay_lsn, (sent_lsn - replay_lsn) AS replication_lag " +
            $"FROM pg_stat_replication;\" 2>&1", ct));

        sb.AppendLine("\n=== AEGIS SNAPSHOT (latest) ===");
        sb.AppendLine(await Cap(
            $"ls -t {SnapDir}/*.snap.json 2>/dev/null | head -1 | " +
            $"xargs -I{{}} sh -c 'echo \"File: {{}}\" && head -60 {{}}' 2>&1", ct));

        return ContextWindow.Trim(sb.ToString());
    }

    static Task<string> Cap(string cmd, CancellationToken ct) =>
        BashBridge.CaptureAsync(cmd, ct);
}