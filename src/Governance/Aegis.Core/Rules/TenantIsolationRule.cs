namespace Aegis.Core.Rules;

using Aegis.Core.Model;

public sealed class TenantIsolationRule : IRule
{
    public string       RuleId   => "AGS-007";
    public string       RuleName => "Tenant Isolation";
    public RuleCategory Category => RuleCategory.Tenant;
    public string       Version  => "1.0";

    public RuleResult Evaluate(ArchitectureModel model)
    {
        var violations = new List<RuleViolation>();

        foreach (var svc in model.Services)
        foreach (var type in svc.Types.Where(t => t.Kind == NodeKind.DbContext))
        {
            var hasTenantFilter = type.Methods.Any(m =>
                m.Name.Contains("Tenant", StringComparison.OrdinalIgnoreCase) ||
                m.Name.Contains("Filter", StringComparison.OrdinalIgnoreCase));

            if (!hasTenantFilter)
            {
                violations.Add(new RuleViolation
                {
                    RuleId      = RuleId,
                    Severity    = RuleSeverity.Warning,
                    ProjectName = svc.ProjectName,
                    Message     = $"DbContext '{type.ShortName}' has no detectable tenant filter method. " +
                                  $"Ensure global query filters enforce tenant isolation.",
                    Subject     = type,
                });
            }
        }

        return new RuleResult { RuleId = RuleId, RuleName = RuleName, Category = Category, Version = Version, Violations = violations };
    }
}