namespace Aegis.Core.Rules;

using Aegis.Core.Model;

public sealed class RuleViolation
{
    public required string         RuleId      { get; init; }
    public required string         Message     { get; init; }
    public required RuleSeverity   Severity    { get; init; }
    public          string?        ProjectName { get; init; }
    public          TypeNode?      Subject     { get; init; }
    public          DependencyEdge? Edge       { get; init; }
}

public sealed class RuleResult
{
    public required string              RuleId     { get; init; }
    public required string              RuleName   { get; init; }
    public required RuleCategory        Category   { get; init; }
    public required string              Version    { get; init; }
    public          List<RuleViolation> Violations { get; init; } = [];
    public          bool                Passed     => Violations.Count == 0;
}

public sealed class EngineReport
{
    public List<RuleResult> Results     { get; init; }
    public DateTime         EvaluatedAt { get; init; }

    public EngineReport(List<RuleResult> results, DateTime evaluatedAt)
    {
        Results     = results;
        EvaluatedAt = evaluatedAt;
    }

    public IEnumerable<RuleViolation> AllViolations =>
        Results.SelectMany(r => r.Violations);

    public int  ErrorCount   => AllViolations.Count(v => v.Severity == RuleSeverity.Error);
    public int  WarningCount => AllViolations.Count(v => v.Severity == RuleSeverity.Warning);
    public bool Passed       => ErrorCount == 0;
}