namespace Aegis.Core.Rules;

using Aegis.Core.Model;

public sealed class ApiLayerRule : IRule
{
    public string       RuleId   => "AGS-003";
    public string       RuleName => "API Layer Rule";
    public RuleCategory Category => RuleCategory.Boundary;
    public string       Version  => "1.0";

    public RuleResult Evaluate(ArchitectureModel model)
    {
        var violations = new List<RuleViolation>();

        foreach (var svc in model.Services)
        foreach (var edge in svc.Edges.Where(e => e.From.Layer == ArchitectureLayer.API))
        {
            if (edge.ToLayer == ArchitectureLayer.Infrastructure)
            {
                violations.Add(new RuleViolation
                {
                    RuleId      = RuleId,
                    Severity    = RuleSeverity.Error,
                    ProjectName = svc.ProjectName,
                    Message     = $"API type '{edge.From.ShortName}' directly depends on " +
                                  $"Infrastructure '{edge.ToFullName}'. Route through Application layer.",
                    Subject     = edge.From,
                    Edge        = edge,
                });
            }
        }

        return new RuleResult { RuleId = RuleId, RuleName = RuleName, Category = Category, Version = Version, Violations = violations };
    }
}