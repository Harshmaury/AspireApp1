namespace Aegis.Core.Rules;

using Aegis.Core.Model;

public sealed class CrossServiceDirectReferenceRule : IRule
{
    public string       RuleId   => "AGS-006";
    public string       RuleName => "Cross-Service Direct Reference";
    public RuleCategory Category => RuleCategory.Boundary;
    public string       Version  => "1.0";

    public RuleResult Evaluate(ArchitectureModel model)
    {
        var violations = new List<RuleViolation>();

        foreach (var edge in model.CrossServiceEdges)
        {
            violations.Add(new RuleViolation
            {
                RuleId      = RuleId,
                Severity    = RuleSeverity.Error,
                ProjectName = edge.From.ProjectName,
                Message     = $"'{edge.From.ShortName}' in [{edge.From.ProjectName}] directly references " +
                              $"'{edge.ToFullName}' in [{edge.ToProjectName}]. " +
                              $"Cross-service communication must go through API Gateway or Kafka.",
                Subject     = edge.From,
                Edge        = edge,
            });
        }

        return new RuleResult { RuleId = RuleId, RuleName = RuleName, Category = Category, Version = Version, Violations = violations };
    }
}