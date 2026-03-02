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
root.AddCommand(VerifyRegionAdapter.Build());
root.AddCommand(new Command("doctor", "PH3: Full platform health"));
root.AddCommand(new Command("tenant", "PH4: Tenant lifecycle"));

return await root.InvokeAsync(args);