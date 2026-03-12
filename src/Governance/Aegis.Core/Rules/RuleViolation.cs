namespace Aegis.Core.Rules;

using Aegis.Core.Model;

public sealed class RuleViolation
{
    public required string          RuleId      { get; init; }
    public required string          Message     { get; init; }
    public required RuleSeverity    Severity    { get; init; }
    public          string?         ProjectName { get; init; }
    public          TypeNode?       Subject     { get; init; }
    public          DependencyEdge? Edge        { get; init; }
}

public sealed class RuleResult
{
    public required string              RuleId     { get; init; }
    public required string              RuleName   { get; init; }
    public required RuleCategory        Category   { get; init; }
    public required string              Version    { get; init; }
    public          List<RuleViolation> Violations { get; init; } = [];

    /// <summary>
    /// A rule passes when it has no actionable violations.
    /// Info-severity entries are observational — they document config-driven
    /// or runtime-only concerns that cannot be statically verified. They must
    /// never cause a rule to fail; doing so creates noise that drowns real errors.
    ///
    /// Severity hierarchy (lowest → highest):
    ///   Info    — informational only, no action required at build time
    ///   Warning — should be addressed, does not block CI
    ///   Error   — must be resolved, blocks CI
    ///
    /// Passed = no Warning or Error violations exist.
    /// </summary>
    public bool Passed => !Violations.Any(v => v.Severity >= RuleSeverity.Warning);

    // Convenience accessors used by renderers and the engine report.
    public int ErrorCount   => Violations.Count(v => v.Severity == RuleSeverity.Error);
    public int WarningCount => Violations.Count(v => v.Severity == RuleSeverity.Warning);
    public int InfoCount    => Violations.Count(v => v.Severity == RuleSeverity.Info);
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

    /// <summary>
    /// The engine report passes when no rule has Error or Warning violations.
    /// Info violations across all rules are surfaced for visibility but never
    /// block the build or mark the report as failed.
    /// </summary>
    public bool Passed => !AllViolations.Any(v => v.Severity >= RuleSeverity.Warning);

    public int ErrorCount   => AllViolations.Count(v => v.Severity == RuleSeverity.Error);
    public int WarningCount => AllViolations.Count(v => v.Severity == RuleSeverity.Warning);
    public int InfoCount    => AllViolations.Count(v => v.Severity == RuleSeverity.Info);
}
