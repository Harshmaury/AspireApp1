namespace Ums.Cli.Adapters;

using System.CommandLine;
using Aegis.Core.Building;
using Aegis.Core.Snapshot;

public static class SnapshotAdapter
{
    private const string SnapshotDir = ".ums/snapshots";

    public static Command Build()
    {
        var cmd = new Command("snapshot", "Architecture snapshot management");
        cmd.AddCommand(BuildCreate());
        cmd.AddCommand(BuildDiff());
        return cmd;
    }

    private static Command BuildCreate()
    {
        var cmd     = new Command("create", "Capture architecture baseline");
        var project = new Option<string>("--project") { IsRequired = true };
        var label   = new Option<string>("--label", () => "baseline");
        cmd.AddOption(project); cmd.AddOption(label);

        cmd.SetHandler(async (proj, lbl) =>
        {
            var model   = await new ArchitectureModelBuilder().BuildAsync(proj);
            var snapDir = Path.Combine(proj, SnapshotDir);
            Directory.CreateDirectory(snapDir);

            var ts     = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var path   = Path.Combine(snapDir, $"{lbl}-{ts}.snap.json");
            await SnapshotStore.SaveAsync(path, model);

            var latest = Path.Combine(snapDir, "baseline-latest.snap.json");
            File.Copy(path, latest, overwrite: true);
            SnapshotStore.Prune(snapDir, prefix: lbl, keepCount: 5);
            Console.WriteLine($"[UMS] Snapshot saved → {path}");
        }, project, label);

        return cmd;
    }

    private static Command BuildDiff()
    {
        var cmd       = new Command("diff", "Diff against saved snapshot");
        var project   = new Option<string>("--project") { IsRequired = true };
        var baseline  = new Option<string>("--baseline") { IsRequired = true };
        var failDrift = new Option<bool>("--fail-on-drift", () => false);
        cmd.AddOption(project); cmd.AddOption(baseline); cmd.AddOption(failDrift);

        cmd.SetHandler(async (proj, base_, fail_) =>
        {
            if (!File.Exists(base_))
            {
                Console.WriteLine("[UMS] No baseline snapshot found — skipping drift check.");
                return;
            }
            var current  = await new ArchitectureModelBuilder().BuildAsync(proj);
            var snapshot = await SnapshotStore.LoadAsync(base_);
            var diff     = SnapshotDiffer.Diff(snapshot, current);

            if (diff.HasDrift)
            {
                Console.WriteLine("[UMS] DRIFT Architecture drift detected:");
                foreach (var (svcName, svcDiff) in diff.ByService.Where(kv => kv.Value.HasDrift).OrderBy(kv => kv.Key))
                {
                    Console.WriteLine($"  SERVICE {svcName}:");
                    foreach (var a in svcDiff.Added)   Console.WriteLine($"    + {a}");
                    foreach (var r in svcDiff.Removed) Console.WriteLine($"    - {r}");
                    foreach (var c in svcDiff.Changed) Console.WriteLine($"    ~ {c}");
                }
                if (fail_) { Console.Error.WriteLine("[UMS] FAIL Drift detected."); Environment.Exit(1); }
            }
            else
            {
                Console.WriteLine("[UMS] PASS No drift — architecture matches baseline.");
            }
        }, project, baseline, failDrift);

        return cmd;
    }
}