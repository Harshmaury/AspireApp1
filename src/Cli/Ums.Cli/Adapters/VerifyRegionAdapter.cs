namespace Ums.Cli.Adapters;

using Aegis.Core.Building;
using Aegis.Core.Config;
using Aegis.Core.Rules;
using System.CommandLine;
using Ums.Cli.Rendering;

public static class VerifyRegionAdapter
{
    public static Command Build()
    {
        var cmd       = new Command("verify-region", "Check multi-region readiness: consumer group scoping and write affinity (AGS-014, AGS-015)");
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

            var threshold = fl != null && Enum.TryParse<RuleSeverity>(fl, true, out var parsed)
                ? parsed
                : RuleSeverity.Warning;

            var builder = new RuleEngineBuilder()
                .AddRule<ConsumerGroupScopingRule>()
                .AddRule<RegionAffinityRule>();

            aegisConfig.Apply(builder);
            foreach (var id in dis.SelectMany(d => d.Split(',', StringSplitOptions.RemoveEmptyEntries)))
                builder.Disable(id.Trim());

            var model  = await new ArchitectureModelBuilder().BuildAsync(proj);
            var report = builder
                .AddRule(new LayerMatrixRule(LayerMatrix.CleanArchitecture()))
    .AddRule(new EventSchemaCompatibilityRule(Path.Combine(proj, "src", ".ums", "event-schemas")))
    .Build().Evaluate(model);
            report     = VerifyDependenciesAdapter.ApplyExceptions(report, aegisConfig);

            var text = RendererFactory.Create(fmt).Render(report);
            Console.Write(text);
            if (out_ != null) await File.WriteAllTextAsync(out_, text);

            ExitCodeResolver.ApplyAndExit(report, threshold, true, "verify-region");

        }, project, output, format, config, failLevel, disable);

        return cmd;
    }
}