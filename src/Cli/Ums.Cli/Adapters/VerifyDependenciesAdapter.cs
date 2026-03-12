namespace Ums.Cli.Adapters;

using Aegis.Core.Building;
using Aegis.Core.Config;
using Aegis.Core.Rules;
using System.CommandLine;
using Ums.Cli.Rendering;

public static class VerifyDependenciesAdapter
{
    public static Command Build()
    {
        var cmd       = new Command("verify-dependencies", "Detect circular, forbidden and cross-service deps");
        var project   = new Option<string>("--project") { IsRequired = true };
        var output    = new Option<string?>("--output");
        var format    = new Option<string>("--format", () => "text");
        var config    = new Option<string?>("--config");
        var failLevel = new Option<string?>("--fail-level");
        var failWarn  = new Option<bool>("--fail-on-warning", () => false);
        var disable   = new Option<string[]>("--disable") { AllowMultipleArgumentsPerToken = true };
        var excludeCat= new Option<string[]>("--exclude-category") { AllowMultipleArgumentsPerToken = true };

        cmd.AddOption(project); cmd.AddOption(output);   cmd.AddOption(format);
        cmd.AddOption(config);  cmd.AddOption(failLevel); cmd.AddOption(failWarn);
        cmd.AddOption(disable); cmd.AddOption(excludeCat);

        cmd.SetHandler(async (proj, out_, fmt, cfg, fl, fw, dis, exCat) =>
        {
            var aegisConfig = await AegisConfig.LoadAsync(cfg ?? AegisConfig.DefaultConfigPath(proj));
            var threshold   = fl != null && Enum.TryParse<RuleSeverity>(fl, true, out var parsed)
                ? parsed : aegisConfig.FailLevel;

            var builder = new RuleEngineBuilder()
                .AddRule<CircularDependencyRule>()
                .AddRule<CrossServiceDirectReferenceRule>();

            aegisConfig.Apply(builder);
            foreach (var id in dis.SelectMany(d => d.Split(',', StringSplitOptions.RemoveEmptyEntries)))
                builder.Disable(id.Trim());

            var model  = await new ArchitectureModelBuilder().BuildAsync(proj);
            var report = builder
                .AddRule(new LayerMatrixRule(LayerMatrix.CleanArchitecture()))
    .AddRule(new EventSchemaCompatibilityRule(Path.Combine(proj, "src", ".ums", "event-schemas")))
    .Build().Evaluate(model);
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
}