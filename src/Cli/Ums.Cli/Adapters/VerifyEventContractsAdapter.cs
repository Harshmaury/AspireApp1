namespace Ums.Cli.Adapters;

using Aegis.Core.Building;
using Aegis.Core.Config;
using Aegis.Core.Rules;
using System.CommandLine;
using Ums.Cli.Rendering;

public static class VerifyEventContractsAdapter
{
    public static Command Build()
    {
        var cmd       = new Command("verify-event-contracts", "Detect breaking changes in integration event schemas (AGS-011)");
        var project   = new Option<string>("--project") { IsRequired = true };
        var schemaDir = new Option<string?>("--schema-dir");
        var output    = new Option<string?>("--output");
        var format    = new Option<string>("--format", () => "text");
        var config    = new Option<string?>("--config");
        var failLevel = new Option<string?>("--fail-level");

        cmd.AddOption(project); cmd.AddOption(schemaDir);
        cmd.AddOption(output);  cmd.AddOption(format);
        cmd.AddOption(config);  cmd.AddOption(failLevel);

        cmd.SetHandler(async (proj, sDir, out_, fmt, cfg, fl) =>
        {
            var aegisConfig   = await AegisConfig.LoadAsync(cfg ?? AegisConfig.DefaultConfigPath(proj));
            var threshold     = fl != null && Enum.TryParse<RuleSeverity>(fl, true, out var parsed)
                ? parsed : aegisConfig.FailLevel;
            var resolvedDir   = sDir ?? Path.Combine(proj, ".ums", "event-schemas");

            var model  = await new ArchitectureModelBuilder().BuildAsync(proj);
            var report = new RuleEngine([new EventSchemaCompatibilityRule(resolvedDir)]).Evaluate(model);
            report     = VerifyDependenciesAdapter.ApplyExceptions(report, aegisConfig);

            var text = RendererFactory.Create(fmt).Render(report);
            Console.Write(text);
            if (out_ != null) await File.WriteAllTextAsync(out_, text);
            ExitCodeResolver.ApplyAndExit(report, threshold, false, "verify-event-contracts");

        }, project, schemaDir, output, format, config, failLevel);

        return cmd;
    }
}