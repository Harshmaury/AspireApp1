namespace Ums.Cli.Adapters;

using System.CommandLine;
using Aegis.Core.Building;
using Aegis.Core.Config;
using Aegis.Core.Rules;
using Ums.Cli.Rendering;

public static class VerifyResilienceAdapter
{
    public static Command Build()
    {
        var cmd       = new Command("verify-resilience", "Check resilience policy coverage (AGS-012, AGS-013)");
        var project   = new Option<string>("--project") { IsRequired = true };
        var output    = new Option<string?>("--output");
        var format    = new Option<string>("--format", () => "text");
        var config    = new Option<string?>("--config");
        var failLevel = new Option<string?>("--fail-level");
        var disable   = new Option<string[]>("--disable") { AllowMultipleArgumentsPerToken = true };

        cmd.AddOption(project); cmd.AddOption(output);
        cmd.AddOption(format);  cmd.AddOption(config);
        cmd.AddOption(failLevel); cmd.AddOption(disable);

        cmd.SetHandler(async (proj, out_, fmt, cfg, fl, dis) =>
        {
            var aegisConfig = await AegisConfig.LoadAsync(cfg ?? AegisConfig.DefaultConfigPath(proj));
            var threshold   = fl != null && Enum.TryParse<RuleSeverity>(fl, true, out var parsed)
                ? parsed : RuleSeverity.Warning;

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