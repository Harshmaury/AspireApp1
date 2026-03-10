namespace Aegis.Core.Rules;

using Aegis.Core.Model;

/// <summary>
/// AGS-016 — Command Validator Coverage
///
/// Every IRequest&lt;T&gt; implementation in the Application layer must have
/// a corresponding AbstractValidator&lt;TRequest&gt; in the same service.
///
/// Rationale:
///   All 9 services use FluentValidation. Without this rule, a developer
///   can add a Command and forget its Validator — the gap is invisible
///   until runtime validation silently passes with no rules applied.
///
/// Severity: Warning (not Error) — missing validator is a quality gap,
///   not an architecture violation, but should block PR approval.
/// </summary>
public sealed class CommandValidatorCoverageRule : IRule
{
    public string       RuleId   => "AGS-016";
    public string       RuleName => "Command Validator Coverage";
    public RuleCategory Category => RuleCategory.Contract;
    public string       Version  => "1.0";

    public RuleResult Evaluate(ArchitectureModel model)
    {
        var violations = new List<RuleViolation>();

        foreach (var svc in model.Services)
        {
            // Collect all IRequest<T> implementations in the Application layer
            var commands = svc.Types
                .Where(t => t.Layer == ArchitectureLayer.Application &&
                            t.Interfaces.Any(i => i.StartsWith("IRequest<",
                                StringComparison.Ordinal) ||
                                i == "IRequest"))
                .ToList();

            if (commands.Count == 0) continue;

            // Collect all AbstractValidator<T> implementations in the same service.
            // TypeNode.BaseTypes contains the base class name (may include generic args).
            // We extract the T from AbstractValidator<T> to get the validated type name.
            var validatedTypes = svc.Types
                .SelectMany(t => t.BaseTypes)
                .Where(bt => bt.StartsWith("AbstractValidator<", StringComparison.Ordinal))
                .Select(bt =>
                {
                    // "AbstractValidator<CreateUserCommand>" → "CreateUserCommand"
                    var start = bt.IndexOf('<') + 1;
                    var end   = bt.LastIndexOf('>');
                    return end > start ? bt[start..end].Trim() : null;
                })
                .Where(name => name is not null)
                .Select(name => name!)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var command in commands)
            {
                // Match by ShortName ("CreateUserCommand") since validators
                // typically live in the same assembly and use simple type names.
                if (!validatedTypes.Contains(command.ShortName))
                {
                    violations.Add(new RuleViolation
                    {
                        RuleId      = RuleId,
                        Severity    = RuleSeverity.Warning,
                        ProjectName = svc.ProjectName,
                        Message     = $"Command '{command.ShortName}' in service '{svc.ProjectName}' " +
                                      $"has no corresponding AbstractValidator<{command.ShortName}>. " +
                                      $"Add a FluentValidation validator in the Application layer.",
                        Subject     = command,
                    });
                }
            }
        }

        return new RuleResult
        {
            RuleId     = RuleId,
            RuleName   = RuleName,
            Category   = Category,
            Version    = Version,
            Violations = violations,
        };
    }
}
