namespace Aegis.Core.Rules;

public sealed class RuleEngineBuilder
{
    private readonly List<IRule>                    _rules     = [];
    private readonly HashSet<string>                _disabled  = [];
    private readonly Dictionary<string, RuleSeverity> _overrides = [];
    private readonly HashSet<RuleCategory>          _excluded  = [];
    private string?                                 _minVersion;

    public RuleEngineBuilder AddRule<T>() where T : IRule, new()
        => AddRule(new T());

    public RuleEngineBuilder AddRule(IRule rule)
    {
        _rules.Add(rule);
        return this;
    }

    public RuleEngineBuilder Disable(string ruleId)
    {
        _disabled.Add(ruleId);
        return this;
    }

    public RuleEngineBuilder WithSeverityOverride(string ruleId, RuleSeverity severity)
    {
        _overrides[ruleId] = severity;
        return this;
    }

    public RuleEngineBuilder ExcludeCategories(params RuleCategory[] categories)
    {
        foreach (var c in categories) _excluded.Add(c);
        return this;
    }

    public RuleEngineBuilder WithMinimumVersion(string minVersion)
    {
        _minVersion = minVersion;
        return this;
    }

    public RuleEngine Build()
    {
        var active = _rules
            .Where(r => !_disabled.Contains(r.RuleId))
            .Where(r => !_excluded.Contains(r.Category))
            .Where(r => _minVersion == null ||
                        string.Compare(r.Version, _minVersion, StringComparison.Ordinal) >= 0)
            .ToList();

        return new RuleEngine(active);
    }
}