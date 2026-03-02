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
        var cmd       = new Command("verify-boundaries", "Enforce Clean Architecture layer rules");
        var project   = new Option<string>("--project") { IsRequired = true };
        var output    = new Option<string?>("--output");
        var format    = new Option<string>("--format", () => "text");
        var config    = new Option<string?>("--config");
        var failLevel = new Option<string?>("--fail-level");
        var failWarn  = new Option<bool>("--fail-on-warning", () => false);
        var disable   = new Option<string[]>("--disable") { AllowMultipleArgumentsPerToken = true };

        cmd.AddOption(project); cmd.AddOption(output);
        cmd.AddOption(format);  cmd.AddOption(config);
        cmd.AddOption(failLevel); cmd.AddOption(failWarn);
        cmd.AddOption(disable);

        cmd.SetHandler(async (proj, out_, fmt, cfg, fl, fw, dis) =>
        {
            var aegisConfig = await AegisConfig.LoadAsync(cfg ?? AegisConfig.DefaultConfigPath(proj));
            var threshold   = fl != null && Enum.TryParse<RuleSeverity>(fl, true, out var parsed)
                ? parsed : aegisConfig.FailLevel;

            var builder = new RuleEngineBuilder()
                .AddRule<DomainIsolationRule>()
                .AddRule<ApplicationLayerRule>()
                .AddRule<ApiLayerRule>()
                .AddRule<SharedKernelIsolationRule>();

            aegisConfig.Apply(builder);
            foreach (var id in dis.SelectMany(d => d.Split(',', StringSplitOptions.RemoveEmptyEntries)))
                builder.Disable(id.Trim());

            var model  = await new ArchitectureModelBuilder().BuildAsync(proj);
            var report = builder.Build().Evaluate(model);
            report     = VerifyDependenciesAdapter.ApplyExceptions(report, aegisConfig);

            var text = RendererFactory.Create(fmt).Render(report);
            Console.Write(text);
            if (out_ != null) await File.WriteAllTextAsync(out_, text);
            ExitCodeResolver.ApplyAndExit(report, threshold, fw, "verify-boundaries");

        }, project, output, format, config, failLevel, failWarn, disable);

        return cmd;
    }
}