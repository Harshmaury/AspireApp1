namespace Aegis.Core.Rules;

using Aegis.Core.Model;

public sealed class SharedKernelIsolationRule : IRule
{
    public string       RuleId   => "AGS-004";
    public string       RuleName => "Shared Kernel Isolation";
    public RuleCategory Category => RuleCategory.Boundary;
    public string       Version  => "1.0";

    public RuleResult Evaluate(ArchitectureModel model)
    {
        var violations = new List<RuleViolation>();

        foreach (var svc in model.Services)
        foreach (var edge in svc.Edges.Where(e => e.From.Layer == ArchitectureLayer.SharedKernel))
        {
            if (edge.ToLayer is ArchitectureLayer.Infrastructure or
                ArchitectureLayer.Application or ArchitectureLayer.API)
            {
                violations.Add(new RuleViolation
                {
                    RuleId      = RuleId,
                    Severity    = RuleSeverity.Error,
                    ProjectName = svc.ProjectName,
                    Message     = $"SharedKernel type '{edge.From.ShortName}' depends on " +
                                  $"[{edge.ToLayer}] '{edge.ToFullName}'. " +
                                  $"SharedKernel must only reference Domain.",
                    Subject     = edge.From,
                    Edge        = edge,
                });
            }
        }

        return new RuleResult { RuleId = RuleId, RuleName = RuleName, Category = Category, Version = Version, Violations = violations };
    }
}