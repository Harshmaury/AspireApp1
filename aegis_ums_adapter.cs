// ============================================================
// Ums.Cli — Adapter Layer (CLI is not the engine)
//
// CLI responsibility: parse args → call Aegis.Core → render output
// Zero business logic. Zero rule evaluation. Pure I/O adapter.
// ============================================================

// ── Program.cs ───────────────────────────────────────────────
using System.CommandLine;
using Ums.Cli.Adapters;
using Microsoft.Build.Locator;

MSBuildLocator.RegisterDefaults();

var root = new RootCommand("ums — UMS Platform Governance CLI");
root.AddCommand(VerifyDependenciesAdapter.Build());
root.AddCommand(VerifyBoundariesAdapter.Build());
root.AddCommand(SnapshotAdapter.Build());

root.AddCommand(VerifyEventContractsAdapter.Build());
root.AddCommand(VerifyResilienceAdapter.Build());
root.AddCommand(new Command("verify-schema-compatibility",  "PH2: Breaking change detection (alias for verify-event-contracts)"));
root.AddCommand(new Command("verify-logging-contract",      "PH3: Log field contract (use verify-resilience --category=Contract)"));
root.AddCommand(new Command("doctor",                       "PH3: Full platform health"));
root.AddCommand(new Command("tenant",                       "PH4: Tenant lifecycle"));

return await root.InvokeAsync(args);

// ── Adapters/VerifyDependenciesAdapter.cs ─────────────────────
namespace Ums.Cli.Adapters;

using System.CommandLine;
using Aegis.Core.Building;
using Aegis.Core.Config;
using Aegis.Core.Rules;
using Ums.Cli.Rendering;

public static class VerifyDependenciesAdapter
{
    public static Command Build()
    {
        var cmd      = new Command("verify-dependencies", "Detect circular, forbidden and cross-service deps");
        var project  = new Option<string>("--project", "Path to .sln file, .csproj file, or root directory") { IsRequired = true };
        var output   = new Option<string?>("--output");
        var format   = new Option<string>("--format", () => "text");
        var config   = new Option<string?>("--config",           "Path to aegis.config.json (auto-detected if omitted)");
        var failLevel= new Option<string?>("--fail-level",       "Minimum severity that fails the build: Info|Warning|Error");
        var failWarn = new Option<bool>("--fail-on-warning",     "Exit code 2 when warnings present (no errors)", () => false);
        var disable  = new Option<string[]>("--disable",         "Comma-separated rule IDs to disable (e.g. AGS-001,AGS-005)")
                           { AllowMultipleArgumentsPerToken = true };
        var excludeCat = new Option<string[]>("--exclude-category", "Rule categories to skip (e.g. Boundary State)")
                           { AllowMultipleArgumentsPerToken = true };

        cmd.AddOption(project);    cmd.AddOption(output);
        cmd.AddOption(format);     cmd.AddOption(config);
        cmd.AddOption(failLevel);  cmd.AddOption(failWarn);
        cmd.AddOption(disable);    cmd.AddOption(excludeCat);

        cmd.SetHandler(async (proj, out_, fmt, cfg, fl, fw, dis, exCat) =>
        {
            var aegisConfig = await AegisConfig.LoadAsync(
                cfg ?? AegisConfig.DefaultConfigPath(proj));

            var threshold = fl != null && Enum.TryParse<RuleSeverity>(fl, true, out var parsed)
                ? parsed
                : aegisConfig.FailLevel;

            var builder = new RuleEngineBuilder()
                .AddRule<CircularDependencyRule>()
                .AddRule<CrossServiceDirectReferenceRule>()
                .AddRule<DomainIsolationRule>();

            aegisConfig.Apply(builder);
            foreach (var id in dis.SelectMany(d => d.Split(',', StringSplitOptions.RemoveEmptyEntries)))
                builder.Disable(id.Trim());
            var cats = exCat.SelectMany(c => c.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(c => Enum.TryParse<RuleCategory>(c.Trim(), true, out var cat) ? (RuleCategory?)cat : null)
                .Where(c => c.HasValue).Select(c => c!.Value).ToArray();
            if (cats.Length > 0) builder.ExcludeCategories(cats);

            var model  = await new ArchitectureModelBuilder().BuildAsync(proj);
            var report = builder.Build().Evaluate(model);
            report     = ApplyExceptions(report, aegisConfig);

            var text = RendererFactory.Create(fmt).Render(report);
            Console.Write(text);
            if (out_ != null) await File.WriteAllTextAsync(out_, text);

            ExitCodeResolver.ApplyAndExit(report, threshold, fw, "verify-dependencies");

        }, project, output, format, config, failLevel, failWarn, disable, excludeCat);

        return cmd;
    }

    internal static EngineReport ApplyExceptions(EngineReport report, AegisConfig cfg) =>
        new(report.Results.Select(r => new RuleResult
        {
            RuleId     = r.RuleId,
            RuleName   = r.RuleName,
            Category   = r.Category,
            Version    = r.Version,
            Violations = r.Violations
                .Select(v => cfg.IsSuppressed(v)
                    ? new RuleViolation { RuleId = v.RuleId, Message = $"[suppressed] {v.Message}",
                                         Severity = RuleSeverity.Info, Subject = v.Subject,
                                         Edge = v.Edge, ProjectName = v.ProjectName }
                    : v)
                .ToList(),
        }).ToList(), report.EvaluatedAt);

// ── Adapters/VerifyBoundariesAdapter.cs ──────────────────────
namespace Ums.Cli.Adapters;

using System.CommandLine;
using Aegis.Core.Building;
using Aegis.Core.Config;
using Aegis.Core.Rules;
using Ums.Cli.Rendering;

public static class VerifyBoundariesAdapter
{
    public static Command Build()
    {
        var cmd      = new Command("verify-boundaries", "Enforce Clean Architecture layer rules");
        var project  = new Option<string>("--project", "Path to .sln file, .csproj file, or root directory") { IsRequired = true };
        var policy   = new Option<string?>("--policy");
        var output   = new Option<string?>("--output");
        var format   = new Option<string>("--format", () => "text");
        var config   = new Option<string?>("--config",           "Path to aegis.config.json");
        var failLevel= new Option<string?>("--fail-level",       "Minimum severity that fails the build: Info|Warning|Error");
        var failWarn = new Option<bool>("--fail-on-warning",     "Exit code 2 when warnings present (no errors)", () => false);
        var disable  = new Option<string[]>("--disable",         "Comma-separated rule IDs to disable")
                           { AllowMultipleArgumentsPerToken = true };
        var excludeCat = new Option<string[]>("--exclude-category", "Rule categories to skip")
                           { AllowMultipleArgumentsPerToken = true };

        cmd.AddOption(project);    cmd.AddOption(policy);
        cmd.AddOption(output);     cmd.AddOption(format);
        cmd.AddOption(config);     cmd.AddOption(failLevel);
        cmd.AddOption(failWarn);   cmd.AddOption(disable);
        cmd.AddOption(excludeCat);

        cmd.SetHandler(async (proj, pol, out_, fmt, cfg, fl, fw, dis, exCat) =>
        {
            var aegisConfig = await AegisConfig.LoadAsync(
                cfg ?? AegisConfig.DefaultConfigPath(proj));

            var threshold = fl != null && Enum.TryParse<RuleSeverity>(fl, true, out var parsed)
                ? parsed
                : aegisConfig.FailLevel;

            var matrix  = pol != null
                ? await PolicyLoader.LoadMatrixAsync(pol)
                : LayerMatrix.CleanArchitecture();

            var builder = new RuleEngineBuilder()
                .AddRule<DomainIsolationRule>()
                .AddRule<ApplicationLayerRule>()
                .AddRule<ApiLayerRule>()
                .AddRule(new LayerMatrixRule(matrix))
                .AddRule<SharedKernelIsolationRule>();

            aegisConfig.Apply(builder);
            foreach (var id in dis.SelectMany(d => d.Split(',', StringSplitOptions.RemoveEmptyEntries)))
                builder.Disable(id.Trim());
            var cats = exCat.SelectMany(c => c.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(c => Enum.TryParse<RuleCategory>(c.Trim(), true, out var cat) ? (RuleCategory?)cat : null)
                .Where(c => c.HasValue).Select(c => c!.Value).ToArray();
            if (cats.Length > 0) builder.ExcludeCategories(cats);

            var model  = await new ArchitectureModelBuilder().BuildAsync(proj);
            var report = builder.Build().Evaluate(model);
            report     = VerifyDependenciesAdapter.ApplyExceptions(report, aegisConfig);

            var text = RendererFactory.Create(fmt).Render(report);
            Console.Write(text);
            if (out_ != null) await File.WriteAllTextAsync(out_, text);

            ExitCodeResolver.ApplyAndExit(report, threshold, fw, "verify-boundaries");

        }, project, policy, output, format, config, failLevel, failWarn, disable, excludeCat);

        return cmd;
    }
}

// ── Adapters/VerifyEventContractsAdapter.cs ──────────────────
namespace Ums.Cli.Adapters;

using System.CommandLine;
using Aegis.Core.Building;
using Aegis.Core.Config;
using Aegis.Core.Rules;
using Ums.Cli.Rendering;

/// <summary>
/// Runs AGS-011 EventSchemaCompatibilityRule.
/// Reads baseline schemas from .ums/event-schemas/ and detects breaking changes.
///
/// Usage:
///   ums verify-event-contracts --project /path/to/root
///   ums verify-event-contracts --project /path/to/root --schema-dir /custom/schemas --fail-level Warning
/// </summary>
public static class VerifyEventContractsAdapter
{
    public static Command Build()
    {
        var cmd       = new Command("verify-event-contracts", "Detect breaking changes in integration event schemas (AGS-011)");
        var project   = new Option<string>("--project", "Path to .sln file, .csproj file, or root directory") { IsRequired = true };
        var schemaDir = new Option<string?>("--schema-dir", "Directory containing baseline .json schema files (default: .ums/event-schemas)");
        var output    = new Option<string?>("--output");
        var format    = new Option<string>("--format", () => "text");
        var config    = new Option<string?>("--config");
        var failLevel = new Option<string?>("--fail-level", "Info|Warning|Error");

        cmd.AddOption(project); cmd.AddOption(schemaDir);
        cmd.AddOption(output);  cmd.AddOption(format);
        cmd.AddOption(config);  cmd.AddOption(failLevel);

        cmd.SetHandler(async (proj, sDir, out_, fmt, cfg, fl) =>
        {
            var aegisConfig = await AegisConfig.LoadAsync(cfg ?? AegisConfig.DefaultConfigPath(proj));
            var threshold   = fl != null && Enum.TryParse<RuleSeverity>(fl, true, out var parsed)
                ? parsed : aegisConfig.FailLevel;

            var resolvedSchemaDir = sDir
                ?? Path.Combine(proj, ".ums", "event-schemas");

            var engine = new RuleEngine([new EventSchemaCompatibilityRule(resolvedSchemaDir)]);
            var model  = await new ArchitectureModelBuilder().BuildAsync(proj);
            var report = engine.Evaluate(model);
            report     = VerifyDependenciesAdapter.ApplyExceptions(report, aegisConfig);

            var text = RendererFactory.Create(fmt).Render(report);
            Console.Write(text);
            if (out_ != null) await File.WriteAllTextAsync(out_, text);

            ExitCodeResolver.ApplyAndExit(report, threshold, false, "verify-event-contracts");

        }, project, schemaDir, output, format, config, failLevel);

        return cmd;
    }
}

// ── Adapters/VerifyResilienceAdapter.cs ───────────────────────
namespace Ums.Cli.Adapters;

using System.CommandLine;
using Aegis.Core.Building;
using Aegis.Core.Config;
using Aegis.Core.Rules;
using Ums.Cli.Rendering;

/// <summary>
/// Runs AGS-012 ResiliencePolicyRule + AGS-013 LoggingContractRule.
/// Both are Phase 3 rules. Default severity: Warning.
///
/// Usage:
///   ums verify-resilience --project /path/to/root
///   ums verify-resilience --project /path/to/root --fail-level Warning --disable AGS-013
/// </summary>
public static class VerifyResilienceAdapter
{
    public static Command Build()
    {
        var cmd       = new Command("verify-resilience", "Check resilience policy coverage and logging contracts (AGS-012, AGS-013)");
        var project   = new Option<string>("--project", "Path to .sln file, .csproj file, or root directory") { IsRequired = true };
        var output    = new Option<string?>("--output");
        var format    = new Option<string>("--format", () => "text");
        var config    = new Option<string?>("--config");
        var failLevel = new Option<string?>("--fail-level", "Info|Warning|Error (default: Warning)");
        var disable   = new Option<string[]>("--disable") { AllowMultipleArgumentsPerToken = true };

        cmd.AddOption(project); cmd.AddOption(output);
        cmd.AddOption(format);  cmd.AddOption(config);
        cmd.AddOption(failLevel); cmd.AddOption(disable);

        cmd.SetHandler(async (proj, out_, fmt, cfg, fl, dis) =>
        {
            var aegisConfig = await AegisConfig.LoadAsync(cfg ?? AegisConfig.DefaultConfigPath(proj));

            // Default threshold for resilience checks is Warning (not Error)
            var threshold = fl != null && Enum.TryParse<RuleSeverity>(fl, true, out var parsed)
                ? parsed
                : RuleSeverity.Warning;

            var builder = new RuleEngineBuilder()
                .AddRule<ResiliencePolicyRule>()
                .AddRule<LoggingContractRule>();

            aegisConfig.Apply(builder);
            foreach (var id in dis.SelectMany(d => d.Split(',', StringSplitOptions.RemoveEmptyEntries)))
                builder.Disable(id.Trim());

            var model  = await new ArchitectureModelBuilder().BuildAsync(proj);
            var report = builder.Build().Evaluate(model);
            report     = VerifyDependenciesAdapter.ApplyExceptions(report, aegisConfig);

            var text = RendererFactory.Create(fmt).Render(report);
            Console.Write(text);
            if (out_ != null) await File.WriteAllTextAsync(out_, text);

            ExitCodeResolver.ApplyAndExit(report, threshold, true, "verify-resilience");

        }, project, output, format, config, failLevel, disable);

        return cmd;
    }
}


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
        var project = new Option<string>("--project", "Path to .sln file, .csproj file, or root directory") { IsRequired = true };
        var label   = new Option<string>("--label", () => "baseline");
        cmd.AddOption(project); cmd.AddOption(label);

        cmd.SetHandler(async (proj, lbl) =>
        {
            var model    = await new ArchitectureModelBuilder().BuildAsync(proj);
            var snapDir  = Path.Combine(proj, SnapshotDir);
            Directory.CreateDirectory(snapDir);

            var ts   = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var path = Path.Combine(snapDir, $"{lbl}-{ts}.snap.json");

            await SnapshotStore.SaveAsync(path, model);

            // Always update baseline-latest pointer
            var latest = Path.Combine(snapDir, "baseline-latest.snap.json");
            File.Copy(path, latest, overwrite: true);

            // Prune — keep 5 most recent per label
            SnapshotStore.Prune(snapDir, prefix: lbl, keepCount: 5);

            Console.WriteLine($"[UMS] Snapshot saved → {path}");
        }, project, label);

        return cmd;
    }

    private static Command BuildDiff()
    {
        var cmd       = new Command("diff", "Diff against saved snapshot");
        var project   = new Option<string>("--project", "Path to .sln file, .csproj file, or root directory") { IsRequired = true };
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

                // Per-service breakdown — only print services that actually changed
                foreach (var (svcName, svcDiff) in diff.ByService.Where(kv => kv.Value.HasDrift).OrderBy(kv => kv.Key))
                {
                    Console.WriteLine($"  SERVICE {svcName}:");
                    foreach (var a in svcDiff.Added)   Console.WriteLine($"    + {a}");
                    foreach (var r in svcDiff.Removed) Console.WriteLine($"    - {r}");
                    foreach (var c in svcDiff.Changed) Console.WriteLine($"    ~ {c}");
                }

                // Fallback to flat list for entries not attributed to a specific service
                // (e.g. added/removed services not in ByService when running against old snapshots)
                var attributedAdded   = diff.ByService.Values.SelectMany(s => s.Added).ToHashSet();
                var attributedRemoved = diff.ByService.Values.SelectMany(s => s.Removed).ToHashSet();
                var attributedChanged = diff.ByService.Values.SelectMany(s => s.Changed).ToHashSet();
                foreach (var a in diff.Added.Where(x => !attributedAdded.Contains(x)))
                    Console.WriteLine($"  + {a}");
                foreach (var r in diff.Removed.Where(x => !attributedRemoved.Contains(x)))
                    Console.WriteLine($"  - {r}");
                foreach (var c in diff.Changed.Where(x => !attributedChanged.Contains(x)))
                    Console.WriteLine($"  ~ {c}");

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

// ── Cli/ExitCodeResolver.cs ───────────────────────────────────
namespace Ums.Cli;

using Aegis.Core.Rules;

/// <summary>
/// Canonical exit code mapping for all Aegis CLI commands.
///
///   0 — clean: no violations at or above threshold
///   1 — errors: one or more Error-severity violations
///   2 — warnings only: no Errors, but warnings present and --fail-on-warning set
///
/// CI pipelines should treat exit 1 as blocking merge.
/// Exit 2 can be treated as advisory or blocking per team policy.
/// </summary>
internal static class ExitCodeResolver
{
    /// Returns the exit code without exiting. Useful for testing.
    public static int Resolve(EngineReport report, RuleSeverity threshold, bool failOnWarning)
    {
        var violations = report.AllViolations
            .Where(v => v.Severity >= threshold)
            .ToList();

        if (violations.Any(v => v.Severity >= RuleSeverity.Error))
            return 1;

        if (failOnWarning && violations.Any(v => v.Severity == RuleSeverity.Warning))
            return 2;

        return 0;
    }

    /// Writes the result line and calls Environment.Exit with the resolved code.
    public static void ApplyAndExit(
        EngineReport report,
        RuleSeverity threshold,
        bool failOnWarning,
        string commandName)
    {
        var code = Resolve(report, threshold, failOnWarning);
        switch (code)
        {
            case 1:
                Console.Error.WriteLine(
                    $"[UMS] FAIL {commandName} — {report.ErrorCount} error(s), {report.WarningCount} warning(s)");
                Environment.Exit(1);
                break;
            case 2:
                Console.Error.WriteLine(
                    $"[UMS] WARN {commandName} — {report.WarningCount} warning(s) (--fail-on-warning is set)");
                Environment.Exit(2);
                break;
            default:
                Console.WriteLine($"[UMS] PASS {commandName}");
                break;
        }
    }
}

// ── Rendering/IReportRenderer.cs ──────────────────────────────
namespace Ums.Cli.Rendering;

using Aegis.Core.Rules;

public interface IReportRenderer
{
    string Render(EngineReport report);
}

// ── Rendering/RendererFactory.cs ──────────────────────────────
namespace Ums.Cli.Rendering;

using Aegis.Core.Rules;

public static class RendererFactory
{
    public static IReportRenderer Create(string format) => format.ToLower() switch
    {
        "json"     => new JsonReportRenderer(),
        "compact"  => new CompactReportRenderer(),
        _          => new TextReportRenderer(),
    };
}

// ── Rendering/TextReportRenderer.cs ───────────────────────────
namespace Ums.Cli.Rendering;

using Aegis.Core.Rules;
using System.Text;

public sealed class TextReportRenderer : IReportRenderer
{
    public string Render(EngineReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"AEGIS_FORMAT:2.0  Evaluated:{report.EvaluatedAt:u}");

        foreach (var r in report.Results)
        {
            var status = r.Passed ? "PASS" : "FAIL";
            sb.AppendLine($"[{status}] [{r.RuleId} v{r.Version}] {r.RuleName}  ({r.Category})");

            foreach (var v in r.Violations)
            {
                var sev = v.Severity switch
                {
                    RuleSeverity.Error   => "ERR ",
                    RuleSeverity.Warning => "WARN",
                    _                   => "INFO",
                };
                sb.AppendLine($"     {sev}  {v.Message}");
                if (v.Subject != null)
                    sb.AppendLine($"          -> {v.Subject.FullName} [{v.Subject.Layer}]");
            }
        }

        sb.AppendLine();
        sb.AppendLine($"SUMMARY  Errors:{report.ErrorCount}  Warnings:{report.WarningCount}  " +
                      $"Status:{(report.Passed ? "PASS" : "FAIL")}");
        return sb.ToString();
    }
}

// ── Rendering/JsonReportRenderer.cs ───────────────────────────
namespace Ums.Cli.Rendering;

using Aegis.Core.Rules;
using System.Text.Json;

public sealed class JsonReportRenderer : IReportRenderer
{
    public string Render(EngineReport report)
    {
        var obj = new
        {
            format      = "AEGIS_FORMAT:2.0",
            evaluatedAt = report.EvaluatedAt,
            passed      = report.Passed,
            errorCount  = report.ErrorCount,
            warnCount   = report.WarningCount,
            rules       = report.Results.Select(r => new
            {
                id         = r.RuleId,
                name       = r.RuleName,
                category   = r.Category.ToString(),
                version    = r.Version,
                passed     = r.Passed,
                violations = r.Violations.Select(v => new
                {
                    severity = v.Severity.ToString(),
                    message  = v.Message,
                    project  = v.ProjectName,
                    subject  = v.Subject?.FullName,
                })
            })
        };
        return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
    }
}

// ── Rendering/CompactReportRenderer.cs ───────────────────────
namespace Ums.Cli.Rendering;

using Aegis.Core.Rules;

/// Compact one-line-per-violation format — ideal for CI log parsing
public sealed class CompactReportRenderer : IReportRenderer
{
    public string Render(EngineReport report) =>
        string.Join("\n", report.AllViolations.Select(v =>
            $"{v.Severity.ToString().ToUpper()}|{v.RuleId}|{v.ProjectName}|{v.Message}"));
}

// ── Snapshot/SnapshotStore.cs ──────────────────────────────────
namespace Aegis.Core.Snapshot;

using Aegis.Core.Model;
using System.Text.Json;

public static class SnapshotStore
{
    private static readonly JsonSerializerOptions _opts =
        new() { WriteIndented = true };

    public static async Task SaveAsync(string path, ArchitectureModel model)
    {
        var services = model.Services.Select(ServiceSnapshot.From).ToList();
        var hash     = ComputeHash(services);

        var snap = new PersistedSnapshot
        {
            FormatVersion = model.FormatVersion,
            CapturedAt    = model.CapturedAt,
            ContentHash   = hash,
            Services      = services,
        };
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(snap, _opts));
    }

    /// SHA-256 over a single service's sorted fingerprint lines.
    /// Used by SnapshotDiffer for O(1) per-service unchanged detection.
    internal static string ComputeServiceHash(ServiceSnapshot svc)
    {
        var lines = svc.Types
            .Select(t => $"{t.FullName}|{t.Layer}|{t.Kind}|{string.Join(",", t.Methods.OrderBy(x => x))}|{string.Join(",", t.Interfaces.OrderBy(x => x))}|{string.Join(",", t.BaseTypes.OrderBy(x => x))}|{string.Join(",", t.Attributes.OrderBy(x => x))}|{string.Join(",", t.Endpoints.OrderBy(x => x))}")
            .Concat(svc.KafkaProducers.Select(k => $"KP:{svc.Name}:{k}"))
            .Concat(svc.KafkaConsumers.Select(k => $"KC:{svc.Name}:{k}"))
            .Concat(svc.DiRegistrations.Select(d => $"DI:{svc.Name}:{d}"))
            .OrderBy(x => x);

        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(string.Join("\n", lines)));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// SHA-256 over all services (whole-model hash for fast total-equality check).
    internal static string ComputeHash(List<ServiceSnapshot> services)
    {
        // Produce a deterministic sorted string of all fingerprints
        var lines = services
            .OrderBy(s => s.Name)
            .SelectMany(s =>
                s.Types.Select(t => $"{t.FullName}|{t.Layer}|{t.Kind}|{string.Join(",", t.Methods.OrderBy(x => x))}|{string.Join(",", t.Interfaces.OrderBy(x => x))}|{string.Join(",", t.BaseTypes.OrderBy(x => x))}|{string.Join(",", t.Attributes.OrderBy(x => x))}|{string.Join(",", t.Endpoints.OrderBy(x => x))}")
                .Concat(s.KafkaProducers.Select(k => $"KP:{s.Name}:{k}"))
                .Concat(s.KafkaConsumers.Select(k => $"KC:{s.Name}:{k}"))
                .Concat(s.DiRegistrations.Select(d => $"DI:{s.Name}:{d}")))
            .OrderBy(x => x);

        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(string.Join("\n", lines)));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static async Task<PersistedSnapshot> LoadAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<PersistedSnapshot>(json)
               ?? throw new InvalidOperationException($"Invalid snapshot: {path}");
    }

    public static void Prune(string dir, string prefix, int keepCount = 5)
    {
        Directory.GetFiles(dir, $"{prefix}-*.snap.json")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Skip(keepCount)
            .ToList()
            .ForEach(f => { File.Delete(f); Console.WriteLine($"[UMS] Pruned: {Path.GetFileName(f)}"); });
    }
}

// ── Snapshot/SnapshotDiffer.cs ────────────────────────────────
namespace Aegis.Core.Snapshot;

using Aegis.Core.Model;

public record DiffResult(bool HasDrift, List<string> Added, List<string> Removed, List<string> Changed)
{
    /// Per-service diff breakdown. Populated by the partial diff path.
    public IReadOnlyDictionary<string, ServiceDiffResult> ByService { get; init; } =
        new Dictionary<string, ServiceDiffResult>();
}

/// Diff result scoped to a single service.
public record ServiceDiffResult(
    string ServiceName,
    bool HasDrift,
    List<string> Added,
    List<string> Removed,
    List<string> Changed);

public static class SnapshotDiffer
{
    public static DiffResult Diff(PersistedSnapshot baseline, ArchitectureModel current)
    {
        // Fast path: if the whole-model hash matches, nothing changed.
        if (baseline.ContentHash != null)
        {
            var currentServices = current.Services.Select(ServiceSnapshot.From).ToList();
            var currentHash     = SnapshotStore.ComputeHash(currentServices);
            if (currentHash == baseline.ContentHash)
                return new DiffResult(false, [], [], []);
        }

        // Partial diff: compute per-service hashes and only fully diff the changed services.
        // This keeps the diff O(changed_services) instead of O(all_types).
        var baselineByService  = baseline.Services.ToDictionary(s => s.Name, StringComparer.Ordinal);
        var currentByService   = current.Services.ToDictionary(s => s.ProjectName, StringComparer.Ordinal);

        var allServiceNames = baselineByService.Keys.Union(currentByService.Keys).ToList();
        var serviceResults  = new Dictionary<string, ServiceDiffResult>();

        var allAdded   = new List<string>();
        var allRemoved = new List<string>();
        var allChanged = new List<string>();

        foreach (var name in allServiceNames)
        {
            var hasBase    = baselineByService.TryGetValue(name, out var baseSvc);
            var hasCurrent = currentByService.TryGetValue(name, out var currSvcModel);

            if (!hasBase)
            {
                // Entire service added
                var added = currSvcModel!.Types.Select(t => t.FullName).ToList();
                serviceResults[name] = new ServiceDiffResult(name, true, added, [], []);
                allAdded.AddRange(added);
                continue;
            }

            if (!hasCurrent)
            {
                // Entire service removed
                var removed = baseSvc!.Types.Select(t => t.FullName).ToList();
                serviceResults[name] = new ServiceDiffResult(name, true, [], removed, []);
                allRemoved.AddRange(removed);
                continue;
            }

            // Both exist — check per-service hash before running full fingerprint diff
            var currentSnapshot = ServiceSnapshot.From(currSvcModel!);
            var currentSvcHash  = SnapshotStore.ComputeServiceHash(currentSnapshot);

            if (baseSvc!.ServiceHash != null && baseSvc.ServiceHash == currentSvcHash)
            {
                serviceResults[name] = new ServiceDiffResult(name, false, [], [], []);
                continue;
            }

            // Hashes differ (or baseline predates per-service hashing) — full fingerprint diff
            var baseSet    = CanonicaliseService(baseSvc);
            var currentSet = CanonicaliseService(currentSnapshot);

            var sAdded   = currentSet.Keys.Except(baseSet.Keys).Select(k => currentSet[k]).ToList();
            var sRemoved = baseSet.Keys.Except(currentSet.Keys).Select(k => baseSet[k]).ToList();
            var sChanged = currentSet.Keys.Intersect(baseSet.Keys)
                .Where(k => currentSet[k] != baseSet[k])
                .Select(k => $"{k} | was: {baseSet[k]} | now: {currentSet[k]}")
                .ToList();

            serviceResults[name] = new ServiceDiffResult(name,
                sAdded.Count > 0 || sRemoved.Count > 0 || sChanged.Count > 0,
                sAdded, sRemoved, sChanged);

            allAdded.AddRange(sAdded);
            allRemoved.AddRange(sRemoved);
            allChanged.AddRange(sChanged);
        }

        return new DiffResult(
            allAdded.Count > 0 || allRemoved.Count > 0 || allChanged.Count > 0,
            allAdded, allRemoved, allChanged)
        {
            ByService = serviceResults,
        };
    }

    // ── Per-service canonicalisation ─────────────────────────────────────────

    private static Dictionary<string, string> CanonicaliseService(ServiceSnapshot svc)
    {
        var dict = svc.Types.ToDictionary(t => t.FullName, t => Fingerprint(t));
        dict[$"__kafka_producers__{svc.Name}"]  = string.Join(",", svc.KafkaProducers.OrderBy(x => x));
        dict[$"__kafka_consumers__{svc.Name}"]  = string.Join(",", svc.KafkaConsumers.OrderBy(x => x));
        dict[$"__di_registrations__{svc.Name}"] = string.Join(",", svc.DiRegistrations.OrderBy(x => x));
        return dict;
    }

    // Stable fingerprint — property order independent
    private static string Fingerprint(TypeNode t) =>
        $"LAYER:{t.Layer}|KIND:{t.Kind}|" +
        $"METHODS:{string.Join(",", t.Methods.Select(m => $"{m.Name}:{m.ReturnType}").OrderBy(x => x))}|" +
        $"INTERFACES:{string.Join(",", t.Interfaces.OrderBy(x => x))}|" +
        $"BASETYPES:{string.Join(",", t.BaseTypes.OrderBy(x => x))}|" +
        $"ATTRIBUTES:{string.Join(",", t.Attributes.OrderBy(x => x))}|" +
        $"ENDPOINTS:{string.Join(",", t.Endpoints.Select(e => $"{e.HttpVerb}:{e.RouteTemplate}").OrderBy(x => x))}";

    private static string Fingerprint(PersistedTypeSnapshot t) =>
        $"LAYER:{t.Layer}|KIND:{t.Kind}|" +
        $"METHODS:{string.Join(",", t.Methods.OrderBy(x => x))}|" +
        $"INTERFACES:{string.Join(",", t.Interfaces.OrderBy(x => x))}|" +
        $"BASETYPES:{string.Join(",", t.BaseTypes.OrderBy(x => x))}|" +
        $"ATTRIBUTES:{string.Join(",", t.Attributes.OrderBy(x => x))}|" +
        $"ENDPOINTS:{string.Join(",", t.Endpoints.OrderBy(x => x))}";
}

// ── Snapshot/PersistedSnapshot.cs ────────────────────────────
namespace Aegis.Core.Snapshot;

public sealed class PersistedSnapshot
{
    public required string FormatVersion  { get; init; }
    public required DateTime CapturedAt  { get; init; }
    /// SHA-256 of the canonicalised fingerprint set.
    /// Enables O(1) equality check before running full diff.
    public string? ContentHash           { get; init; }
    public List<ServiceSnapshot> Services { get; init; } = [];
}

public sealed class ServiceSnapshot
{
    public required string Name  { get; init; }
    /// SHA-256 of this service's canonicalised fingerprint. Enables O(1) per-service unchanged check.
    public string? ServiceHash              { get; init; }
    public List<PersistedTypeSnapshot> Types { get; init; } = [];
    public List<string> KafkaProducers  { get; init; } = [];
    public List<string> KafkaConsumers  { get; init; } = [];
    public List<string> DiRegistrations { get; init; } = [];

    public static ServiceSnapshot From(Aegis.Core.Model.ServiceModel s)
    {
        var snap = new ServiceSnapshot
        {
            Name  = s.ProjectName,
            Types = s.Types.Select(t => new PersistedTypeSnapshot
            {
                FullName   = t.FullName,
                Layer      = t.Layer.ToString(),
                Kind       = t.Kind.ToString(),
                Methods    = t.Methods.Select(m => $"{m.Name}:{m.ReturnType}").ToList(),
                Interfaces = t.Interfaces.ToList(),
                BaseTypes  = t.BaseTypes.ToList(),
                Attributes = t.Attributes.ToList(),
                Endpoints  = t.Endpoints.Select(e => $"{e.HttpVerb}:{e.RouteTemplate}").ToList(),
            }).ToList(),
            KafkaProducers  = s.KafkaProducers.Select(k => $"{k.ProducerClass}:{k.EventFullName}").ToList(),
            KafkaConsumers  = s.KafkaConsumers.Select(k => $"{k.ConsumerClass}:{string.Join(",", k.EventTypes)}").ToList(),
            DiRegistrations = s.DiRegistrations.Select(d => $"{d.Lifetime}:{d.ServiceType}->{d.ImplementationType}").ToList(),
        };
        // Stamp per-service hash immediately so the differ can use it for fast-path checks
        return snap with { ServiceHash = SnapshotStore.ComputeServiceHash(snap) };
    }
}

public sealed class PersistedTypeSnapshot
{
    public required string       FullName   { get; init; }
    public required string       Layer      { get; init; }
    public required string       Kind       { get; init; }
    public List<string>          Methods    { get; init; } = [];
    public List<string>          Interfaces { get; init; } = [];
    public List<string>          BaseTypes  { get; init; } = [];
    public List<string>          Attributes { get; init; } = [];
    public List<string>          Endpoints  { get; init; } = [];
    public List<string>          KafkaProducers  { get; init; } = [];
    public List<string>          KafkaConsumers  { get; init; } = [];
    public List<string>          DiRegistrations { get; init; } = [];
}
