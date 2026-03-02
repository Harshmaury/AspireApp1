namespace Aegis.Core.Rules;

using Aegis.Core.Model;

public sealed class DomainIsolationRule : IRule
{
    public string       RuleId   => "AGS-001";
    public string       RuleName => "Domain Layer Isolation";
    public RuleCategory Category => RuleCategory.Boundary;
    public string       Version  => "1.0";

    public RuleResult Evaluate(ArchitectureModel model)
    {
        var violations = new List<RuleViolation>();

        foreach (var svc in model.Services)
        foreach (var edge in svc.Edges.Where(e => e.From.Layer == ArchitectureLayer.Domain))
        {
            if (edge.ToLayer is ArchitectureLayer.Infrastructure or ArchitectureLayer.API)
            {
                violations.Add(new RuleViolation
                {
                    RuleId      = RuleId,
                    Severity    = RuleSeverity.Error,
                    ProjectName = svc.ProjectName,
                    Message     = $"Domain type '{edge.From.ShortName}' depends on " +
                                  $"[{edge.ToLayer}] '{edge.ToFullName}'. " +
                                  $"Domain must not reference Infrastructure or API layers.",
                    Subject     = edge.From,
                    Edge        = edge,
                });
            }
        }

        return new RuleResult { RuleId = RuleId, RuleName = RuleName, Category = Category, Version = Version, Violations = violations };
    }
}