namespace Aegis.Core.Rules;

using Aegis.Core.Model;

public sealed class ResiliencePolicyRule : IRule
{
    public string       RuleId   => "AGS-012";
    public string       RuleName => "Resilience Policy Coverage";
    public RuleCategory Category => RuleCategory.Resilience;
    public string       Version  => "1.0";

    public RuleResult Evaluate(ArchitectureModel model)
    {
        var violations = new List<RuleViolation>();

        foreach (var svc in model.Services)
        foreach (var reg in svc.DiRegistrations.Where(d => d.Lifetime == DiLifetime.Transient))
        {
            if (reg.ServiceType.Contains("HttpClient") && !reg.HasResiliencePolicy)
            {
                violations.Add(new RuleViolation
                {
                    RuleId      = RuleId,
                    Severity    = RuleSeverity.Warning,
                    ProjectName = svc.ProjectName,
                    Message     = $"HttpClient '{reg.ServiceType}' has no resilience policy. " +
                                  $"Add AddResilienceHandler() or AddTransientHttpErrorPolicy().",
                });
            }
        }

        return new RuleResult { RuleId = RuleId, RuleName = RuleName, Category = Category, Version = Version, Violations = violations };
    }
}