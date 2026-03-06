using System.Text;
using Ums.Cli.Infrastructure;

namespace Ums.Cli.AI;

/// <summary>
/// Gathers live cluster state from kubectl, Kafka, and Aegis snapshots.
/// Each section is labelled so Claude can parse it reliably.
/// </summary>
public sealed class ContextCollector
{
    const string Ns       = "ums";
    const string SnapDir  = "/mnt/c/Users/harsh/source/repos/AspireApp1/src/.ums/snapshots";
    const int    LogTail  = 60;

    // All known service → pod-label mappings (from services.conf)
    static readonly Dictionary<string, string> PodLabels =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["identity"]    = "identity-api",
            ["academic"]    = "academic-api",
            ["student"]     = "student-api",
            ["attendance"]  = "attendance-api",
            ["examination"] = "examination-api",
            ["exam"]        = "examination-api",
            ["fee"]         = "fee-api",
            ["faculty"]     = "faculty-api",
            ["hostel"]      = "hostel-api",
            ["notification"]= "notification-api",
            ["gateway"]     = "api-gateway",
            ["bff"]         = "bff",
            ["kafka"]       = "kafka",
            ["postgres"]    = "postgres",
            ["zookeeper"]   = "zookeeper",
        };

    /// <param name="focusService">
    ///   Optional service name — activates targeted log collection and skips
    ///   the broad restart-scan to stay within the token budget.
    /// </param>
    public async Task<string> CollectAsync(
        string? focusService = null,
        CancellationToken ct = default)
    {
        var sb = new StringBuilder();

        // ── 1. Pod overview ──────────────────────────────────────────────────
        sb.AppendLine("=== POD STATUS ===");
        sb.AppendLine(await Cap($"kubectl get pods -n {Ns} -o wide --no-headers 2>&1", ct));

        // ── 2. Warning events (most actionable signals) ─────────────────────
        sb.AppendLine("\n=== RECENT WARNING EVENTS ===");
        sb.AppendLine(await Cap(
            $"kubectl get events -n {Ns} --field-selector type=Warning " +
            $"--sort-by='.lastTimestamp' 2>&1 | tail -20", ct));

        // ── 3. Service logs ─────────────────────────────────────────────────
        if (focusService != null && PodLabels.TryGetValue(focusService, out var lbl))
        {
            sb.AppendLine($"\n=== {lbl.ToUpper()} LOGS (last {LogTail} lines) ===");
            sb.AppendLine(await Cap(
                $"kubectl logs -n {Ns} -l app={lbl} --tail={LogTail} " +
                $"--all-containers --prefix 2>&1", ct));

            // Also grab previous container logs if the pod has restarted
            sb.AppendLine($"\n=== {lbl.ToUpper()} PREVIOUS CONTAINER LOGS ===");
            sb.AppendLine(await Cap(
                $"kubectl logs -n {Ns} -l app={lbl} --tail=30 " +
                $"--previous --all-containers 2>&1", ct));
        }
        else
        {
            // Grab the last 30 lines from every pod that has ever restarted
            sb.AppendLine("\n=== RESTARTED POD LOGS ===");
            var restarted = await Cap(
                $"kubectl get pods -n {Ns} --no-headers 2>&1 | awk '$4 > 0 {{print $1}}' | head -4",
                ct);

            foreach (var pod in restarted.Split('\n',
                         StringSplitOptions.RemoveEmptyEntries |
                         StringSplitOptions.TrimEntries))
            {
                sb.AppendLine($"--- {pod} (restarted) ---");
                sb.AppendLine(await Cap(
                    $"kubectl logs -n {Ns} {pod} --tail=30 --previous 2>&1", ct));
            }
        }

        // ── 4. Kafka consumer lag ───────────────────────────────────────────
        sb.AppendLine("\n=== KAFKA CONSUMER LAG ===");
        sb.AppendLine(await Cap(
            $"kubectl exec -n {Ns} deploy/kafka -- " +
            $"kafka-consumer-groups.sh --bootstrap-server localhost:9092 " +
            $"--describe --all-groups 2>&1 | head -50", ct));

        // ── 5. Aegis snapshot (latest) ──────────────────────────────────────
        sb.AppendLine("\n=== AEGIS SNAPSHOT (latest) ===");
        // Print filename + first 60 lines of the JSON (rules + violations visible)
        sb.AppendLine(await Cap(
            $"ls -t {SnapDir}/*.snap.json 2>/dev/null | head -1 | " +
            $"xargs -I{{}} sh -c 'echo \"File: {{}}\" && head -60 {{}}' 2>&1", ct));

        return ContextWindow.Trim(sb.ToString());
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    static Task<string> Cap(string cmd, CancellationToken ct) =>
        BashBridge.CaptureAsync(cmd, ct);
}