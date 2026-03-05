namespace Aegis.Core.Rules;
using Aegis.Core.Model;
public sealed class LoggingContractRule : IRule
{
    public string       RuleId   => "AGS-013";
    public string       RuleName => "Logging Contract";
    public RuleCategory Category => RuleCategory.Contract;
    public string       Version  => "1.0";

    public RuleResult Evaluate(ArchitectureModel model)
    {
        var violations = new List<RuleViolation>();

        foreach (var svc in model.Services)
        {
            // Match MVC controllers OR minimal-API endpoint classes (API layer with Endpoints populated)
            var entryPoints = svc.Types.Where(t =>
                t.Kind == NodeKind.Controller ||
                (t.Layer == ArchitectureLayer.API && t.Endpoints.Count > 0));

            foreach (var type in entryPoints)
            {
                var hasLogger = svc.Edges
                    .Where(e => e.From.FullName == type.FullName &&
                                e.Kind == EdgeKind.ConstructorInjection)
                    .Any(e => e.ToFullName.Contains("ILogger"));

                if (!hasLogger)
                {
                    violations.Add(new RuleViolation
                    {
                        RuleId      = RuleId,
                        Severity    = RuleSeverity.Warning,
                        ProjectName = svc.ProjectName,
                        Message     = $"Endpoint class '{type.ShortName}' does not inject ILogger. " +
                                      $"All API entry points must log structured requests.",
                        Subject     = type,
                    });
                }
            }
        }

        return new RuleResult { RuleId = RuleId, RuleName = RuleName, Category = Category, Version = Version, Violations = violations };
    }
}
