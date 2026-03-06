namespace Aegis.Core.Rules;
using Aegis.Core.Model;

public sealed class MediatorPatternRule : IRule
{
    public string       RuleId   => "AGS-010";
    public string       RuleName => "Mediator Pattern Enforcement";
    public RuleCategory Category => RuleCategory.Boundary;
    public string       Version  => "1.0";

    private static readonly HashSet<string> _mediatorTypes =
        ["IMediator", "ISender", "IPublisher"];

    public RuleResult Evaluate(ArchitectureModel model)
    {
        var violations = new List<RuleViolation>();

        foreach (var svc in model.Services)
        foreach (var type in svc.Types.Where(t => t.Kind == NodeKind.Controller))
        {
            var ctorDeps = svc.Edges
                .Where(e => e.From.FullName == type.FullName && e.Kind == EdgeKind.ConstructorInjection)
                .ToList();

            var hasMediator = ctorDeps.Any(e =>
                _mediatorTypes.Any(m => e.ToFullName.Contains(m, StringComparison.OrdinalIgnoreCase)));

            // Direct Application service injection without going through mediator
            var directAppServices = ctorDeps
                .Where(e => e.ToLayer == ArchitectureLayer.Application &&
                            !_mediatorTypes.Any(m => e.ToFullName.Contains(m, StringComparison.OrdinalIgnoreCase)) &&
                            !e.ToFullName.Contains("ILogger") &&
                            !e.ToFullName.Contains("IMapper") &&
                            !e.ToFullName.Contains("IConfiguration"))
                .ToList();

            foreach (var dep in directAppServices)
                violations.Add(new RuleViolation
                {
                    RuleId      = RuleId,
                    Severity    = RuleSeverity.Warning,
                    ProjectName = svc.ProjectName,
                    Message     = $"Controller '{type.ShortName}' directly injects Application service " +
                                  $"'{dep.ToFullName}'. Prefer IMediator/ISender to keep controllers thin " +
                                  $"and decouple from Application layer implementation details.",
                    Subject     = type,
                    Edge        = dep,
                });
        }

        return new RuleResult { RuleId = RuleId, RuleName = RuleName, Category = Category, Version = Version, Violations = violations };
    }
}



