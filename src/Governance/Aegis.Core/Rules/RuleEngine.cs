namespace Aegis.Core.Rules;

using Aegis.Core.Model;

public sealed class RuleEngine
{
    private readonly IReadOnlyList<IRule> _rules;

    public RuleEngine(IReadOnlyList<IRule> rules)
    {
        _rules = rules;
    }

    public EngineReport Evaluate(ArchitectureModel model)
    {
        var results = _rules
            .Select(r => r.Evaluate(model))
            .ToList();

        return new EngineReport(results, DateTime.UtcNow);
    }
}