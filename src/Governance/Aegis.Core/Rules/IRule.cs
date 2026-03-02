namespace Aegis.Core.Rules;

using Aegis.Core.Model;

public interface IRule
{
    string       RuleId   { get; }
    string       RuleName { get; }
    RuleCategory Category { get; }
    string       Version  { get; }
    RuleResult   Evaluate(ArchitectureModel model);
}

public enum RuleCategory
{
    Dependency,
    Boundary,
    Messaging,
    State,
    Resilience,
    Contract,
    Tenant,
}

public enum RuleSeverity
{
    Info    = 0,
    Warning = 1,
    Error   = 2,
}