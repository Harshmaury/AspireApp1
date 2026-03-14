namespace Ums.Cli.Commands;

using System.CommandLine;
using Aegis.Core.Building;
using Aegis.Core.Model;
using Aegis.Core.Config;
using Aegis.Core.Rules;
using Aegis.Core.Snapshot;
using Spectre.Console;
using Ums.Cli.Adapters;
using Ums.Cli.Infrastructure;
using Ums.Cli.Rendering;

public static class GovernCommands
{
    private static string DefaultProject =>
        Environment.GetEnvironmentVariable("UMS_REPO_ROOT")
        ?? Directory.GetCurrentDirectory();

    private static string SnapshotDir =>
        Path.Combine(DefaultProject, "src", ".ums", "snapshots");

    public static Command Build()
    {
        var govern = new Command("govern", "Architecture governance (Aegis rules, snapshots, reports)");
        govern.AddCommand(BuildVerify());
        govern.AddCommand(BuildSnapshot());
        govern.AddCommand(BuildReport());
        return govern;
    }

    // â”€â”€ verify â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static Command BuildVerify()
    {
        var verify = new Command("verify", "Run Aegis governance rules");
        verify.AddCommand(BuildVerifyAll());
        verify.AddCommand(BuildVerifyOne("boundaries",
            "Enforce Clean Architecture layer boundaries",
            b => b.AddRule<DomainIsolationRule>()
                  .AddRule<ApplicationLayerRule>()
                  .AddRule<ApiLayerRule>()
                  .AddRule<SharedKernelIsolationRule>()));
        verify.AddCommand(BuildVerifyOne("dependencies",
            "Detect circular and cross-service direct references",
            b => b.AddRule<CircularDependencyRule>()
                  .AddRule<CrossServiceDirectReferenceRule>()
                  .AddRule<DomainIsolationRule>()));
        verify.AddCommand(BuildVerifyOne("tenant",
            "Verify tenant isolation across all services",
            b => b.AddRule<TenantIsolationRule>()));
        verify.AddCommand(BuildVerifyOne("resilience",
            "Verify resilience policies and logging contracts",
            b => b.AddRule<ResiliencePolicyRule>().AddRule<LoggingContractRule>()));
        verify.AddCommand(BuildVerifyOne("region",
            "Verify multi-region readiness",
            b => b.AddRule<ConsumerGroupScopingRule>().AddRule<RegionAffinityRule>()));
        return verify;
    }

    private static Command BuildVerifyAll()
    {
        var cmd      = new Command("all", "Run every Aegis rule");
        var project  = ProjectOption();
        var format   = FormatOption();
        var output   = OutputOption();
        var failWarn = new Option<bool>("--fail-on-warning", () => false);
        cmd.AddOption(project); cmd.AddOption(format);
        cmd.AddOption(output);  cmd.AddOption(failWarn);

        cmd.SetHandler(async (proj, fmt, out_, fw) =>
        {
            AnsiConsole.MarkupLine("[bold cyan]  Aegis -- Full Governance Check[/]");
            var cfg = await AegisConfig.LoadAsync(AegisConfig.DefaultConfigPath(proj));
            var builder = new RuleEngineBuilder()
                .AddRule<DomainIsolationRule>()
                .AddRule<ApplicationLayerRule>()
                .AddRule<ApiLayerRule>()
                .AddRule<SharedKernelIsolationRule>()
                .AddRule<CircularDependencyRule>()
                .AddRule<CrossServiceDirectReferenceRule>()
                .AddRule<TenantIsolationRule>()
                .AddRule<ResiliencePolicyRule>()
                .AddRule<LoggingContractRule>()
                .AddRule<ConsumerGroupScopingRule>()
                .AddRule<RegionAffinityRule>();
            cfg.Apply(builder);

            ArchitectureModel model = null!;
            await AnsiConsole.Status().StartAsync("Building architecture model...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                model = await new ArchitectureModelBuilder().BuildAsync(proj);
            });

            var report = builder
                .AddRule(new LayerMatrixRule(LayerMatrix.CleanArchitecture()))
                .AddRule(new EventSchemaCompatibilityRule(
                    Path.Combine(proj, "src", ".ums", "event-schemas")))
                .Build().Evaluate(model);
            report = VerifyDependenciesAdapter.ApplyExceptions(report, cfg);
            var text = RendererFactory.Create(fmt).Render(report);
            Console.Write(text);
            if (out_ != null) await File.WriteAllTextAsync(out_, text);
            PrintSummary(report);
            ExitCodeResolver.ApplyAndExit(report, cfg.FailLevel, fw, "verify all");
        }, project, format, output, failWarn);

        return cmd;
    }

    private static Command BuildVerifyOne(
        string name, string description,
        Func<RuleEngineBuilder, RuleEngineBuilder> configure)
    {
        var cmd      = new Command(name, description);
        var project  = ProjectOption();
        var format   = FormatOption();
        var output   = OutputOption();
        var failWarn = new Option<bool>("--fail-on-warning", () => false);
        cmd.AddOption(project); cmd.AddOption(format);
        cmd.AddOption(output);  cmd.AddOption(failWarn);

        cmd.SetHandler(async (proj, fmt, out_, fw) =>
        {
            AnsiConsole.MarkupLine($"[bold cyan]  Aegis -- verify {name}[/]");
            var cfg     = await AegisConfig.LoadAsync(AegisConfig.DefaultConfigPath(proj));
            var builder = configure(new RuleEngineBuilder());
            cfg.Apply(builder);

            ArchitectureModel model = null!;
            await AnsiConsole.Status().StartAsync("Building architecture model...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                model = await new ArchitectureModelBuilder().BuildAsync(proj);
            });

            var report = builder
            .AddRule(new LayerMatrixRule(LayerMatrix.CleanArchitecture())).Build().Evaluate(model);
            report = VerifyDependenciesAdapter.ApplyExceptions(report, cfg);
            var text = RendererFactory.Create(fmt).Render(report);
            Console.Write(text);
            if (out_ != null) await File.WriteAllTextAsync(out_, text);
            PrintSummary(report);
            ExitCodeResolver.ApplyAndExit(report, cfg.FailLevel, fw, $"verify {name}");
        }, project, format, output, failWarn);

        return cmd;
    }

    // â”€â”€ snapshot â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static Command BuildSnapshot()
    {
        var snap = new Command("snapshot", "Architecture snapshot management");
        snap.AddCommand(BuildSnapCreate());
        snap.AddCommand(BuildSnapDiff());
        snap.AddCommand(BuildSnapList());
        return snap;
    }

    private static Command BuildSnapCreate()
    {
        var cmd     = new Command("create", "Capture a named architecture snapshot");
        var label   = new Argument<string>("label", () => "baseline", "Snapshot label");
        var project = ProjectOption();
        cmd.AddArgument(label); cmd.AddOption(project);

        cmd.SetHandler(async (lbl, proj) =>
        {
            Directory.CreateDirectory(SnapshotDir);
            ArchitectureModel model = null!;
            await AnsiConsole.Status().StartAsync("Building architecture model...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                model = await new ArchitectureModelBuilder().BuildAsync(proj);
            });
            var ts     = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var path   = Path.Combine(SnapshotDir, $"{lbl}-{ts}.snap.json");
            await SnapshotStore.SaveAsync(path, model);
            var latest = Path.Combine(SnapshotDir, $"{lbl}-latest.snap.json");
            File.Copy(path, latest, overwrite: true);
            SnapshotStore.Prune(SnapshotDir, prefix: lbl, keepCount: 5);
            AnsiConsole.MarkupLine($"[green]  Snapshot saved --> {path}[/]");
        }, label, project);

        return cmd;
    }

    private static Command BuildSnapDiff()
    {
        var cmd       = new Command("diff", "Diff current architecture against a saved snapshot");
        var baseline  = new Argument<string?>("baseline", () => null, "Snapshot file (default: baseline-latest)");
        var project   = ProjectOption();
        var failDrift = new Option<bool>("--fail-on-drift", () => false);
        cmd.AddArgument(baseline); cmd.AddOption(project); cmd.AddOption(failDrift);

        cmd.SetHandler(async (bl, proj, fail) =>
        {
            var snapPath = bl ?? Path.Combine(SnapshotDir, "baseline-latest.snap.json");
            if (!File.Exists(snapPath))
            {
                AnsiConsole.MarkupLine("[yellow]  No baseline snapshot found. Run: ums govern snapshot create[/]");
                return;
            }
            ArchitectureModel model = null!;
            await AnsiConsole.Status().StartAsync("Building architecture model...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                model = await new ArchitectureModelBuilder().BuildAsync(proj);
            });
            var snapshot = await SnapshotStore.LoadAsync(snapPath);
            var diff     = SnapshotDiffer.Diff(snapshot, model);
            if (!diff.HasDrift)
            {
                AnsiConsole.MarkupLine("[green]  PASS -- No architecture drift detected[/]");
                return;
            }
            AnsiConsole.MarkupLine("[yellow]  DRIFT -- Architecture changes detected:[/]");
            foreach (var (svcName, svcDiff) in diff.ByService
                .Where(kv => kv.Value.HasDrift).OrderBy(kv => kv.Key))
            {
                AnsiConsole.MarkupLine($"\n  [bold]{svcName}[/]");
                foreach (var a in svcDiff.Added)   AnsiConsole.MarkupLine($"    [green]+ {a}[/]");
                foreach (var r in svcDiff.Removed) AnsiConsole.MarkupLine($"    [red]- {r}[/]");
                foreach (var c in svcDiff.Changed) AnsiConsole.MarkupLine($"    [yellow]~ {c}[/]");
            }
            if (fail) { Console.Error.WriteLine("[UMS] FAIL Drift detected."); Environment.Exit(1); }
        }, baseline, project, failDrift);

        return cmd;
    }

    private static Command BuildSnapList()
    {
        var cmd = new Command("list", "List all saved snapshots");
        cmd.SetHandler(() =>
        {
            if (!Directory.Exists(SnapshotDir))
            {
                AnsiConsole.MarkupLine("[dim]  No snapshots found.[/]");
                return Task.CompletedTask;
            }
            var files = Directory.GetFiles(SnapshotDir, "*.snap.json")
                .OrderByDescending(File.GetLastWriteTimeUtc).ToList();
            if (files.Count == 0) { AnsiConsole.MarkupLine("[dim]  No snapshots found.[/]"); return Task.CompletedTask; }
            var table = new Table().BorderColor(Color.Grey);
            table.AddColumn("Label"); table.AddColumn("Created"); table.AddColumn("Path");
            foreach (var f in files)
                table.AddRow(Path.GetFileNameWithoutExtension(f),
                    File.GetLastWriteTime(f).ToString("yyyy-MM-dd HH:mm"), f);
            AnsiConsole.Write(table);
            return Task.CompletedTask;
        });
        return cmd;
    }

    // â”€â”€ report â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static Command BuildReport()
    {
        var cmd     = new Command("report", "Generate a full governance report to file");
        var format  = FormatOption();
        var output  = OutputOption();
        var project = ProjectOption();
        cmd.AddOption(format); cmd.AddOption(output); cmd.AddOption(project);

        cmd.SetHandler(async (fmt, out_, proj) =>
        {
            var cfg = await AegisConfig.LoadAsync(AegisConfig.DefaultConfigPath(proj));
            var builder = new RuleEngineBuilder()
                .AddRule<DomainIsolationRule>().AddRule<ApplicationLayerRule>()
                .AddRule<ApiLayerRule>().AddRule<SharedKernelIsolationRule>()
                .AddRule<CircularDependencyRule>().AddRule<CrossServiceDirectReferenceRule>()
                .AddRule<TenantIsolationRule>().AddRule<ResiliencePolicyRule>()
                .AddRule<LoggingContractRule>().AddRule<ConsumerGroupScopingRule>()
                .AddRule<RegionAffinityRule>();
            cfg.Apply(builder);

            ArchitectureModel model = null!;
            await AnsiConsole.Status().StartAsync("Building architecture model...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                model = await new ArchitectureModelBuilder().BuildAsync(proj);
            });

            var report = builder
                .AddRule(new LayerMatrixRule(LayerMatrix.CleanArchitecture()))
    .AddRule(new EventSchemaCompatibilityRule(Path.Combine(proj, "src", ".ums", "event-schemas")))
    .Build().Evaluate(model);
            report = VerifyDependenciesAdapter.ApplyExceptions(report, cfg);
            var text = RendererFactory.Create(fmt).Render(report);
            var dest = out_ ?? $"governance-report-{DateTime.Now:yyyyMMdd-HHmmss}.{fmt}";
            await File.WriteAllTextAsync(dest, text);
            AnsiConsole.MarkupLine($"[green]  Report saved --> {dest}[/]");
        }, format, output, project);

        return cmd;
    }

    // â”€â”€ helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static Option<string> ProjectOption() =>
        new("--project", () => DefaultProject, "Path to .sln, .csproj, or root directory");

    private static Option<string> FormatOption() =>
        new("--format", () => "text", "Output format: text | json | compact");

    private static Option<string?> OutputOption() =>
        new Option<string?>("--output", "Write output to file instead of stdout");

    private static void PrintSummary(EngineReport report)
    {
        var icon  = report.Passed ? "[green]PASS[/]" : "[red]FAIL[/]";
        var errs  = report.ErrorCount > 0   ? $"[red]{report.ErrorCount} error(s)[/]"      : "[dim]0 errors[/]";
        var warns = report.WarningCount > 0 ? $"[yellow]{report.WarningCount} warning(s)[/]" : "[dim]0 warnings[/]";
        AnsiConsole.MarkupLine($"\n  {icon}  {errs}  {warns}");
    }
}

