using Microsoft.Build.Locator;
using System.CommandLine;
using Ums.Cli.Adapters;
using Ums.Cli.Commands;

// MSBuild SDK location — must be set before RegisterDefaults()
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_ROOT")))
    Environment.SetEnvironmentVariable("DOTNET_ROOT", "/usr/lib/dotnet");

MSBuildLocator.RegisterDefaults();

var root = new RootCommand("ums — UMS Platform CLI  (infra · govern · context · git · ai)");

// ── core domains ──────────────────────────────────────────────────────────
root.AddCommand(InfraCommands.Build());
root.AddCommand(GovernCommands.Build());
root.AddCommand(ContextCommands.Build());
root.AddCommand(GitCommands.Build());
root.AddCommand(AiCommands.Build());

// ── legacy top-level aliases (CI backward-compat) ────────────────────────
root.AddCommand(VerifyDependenciesAdapter.Build());
root.AddCommand(VerifyBoundariesAdapter.Build());
root.AddCommand(SnapshotAdapter.Build());
root.AddCommand(VerifyEventContractsAdapter.Build());
root.AddCommand(VerifyResilienceAdapter.Build());
root.AddCommand(VerifyRegionAdapter.Build());

return await root.InvokeAsync(args);
