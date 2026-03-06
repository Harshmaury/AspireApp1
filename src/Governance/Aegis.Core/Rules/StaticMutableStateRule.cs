namespace Aegis.Core.Rules;
using Aegis.Core.Model;

public sealed class StaticMutableStateRule : IRule
{
    public string       RuleId   => "AGS-009";
    public string       RuleName => "Static Mutable State in Core Layers";
    public RuleCategory Category => RuleCategory.State;
    public string       Version  => "1.0";

    private static readonly HashSet<string> _mutableCollections =
        ["List", "Dictionary", "HashSet", "ConcurrentDictionary",
         "ConcurrentBag", "ConcurrentQueue", "Queue", "Stack"];

    public RuleResult Evaluate(ArchitectureModel model)
    {
        var violations = new List<RuleViolation>();

        foreach (var svc in model.Services)
        foreach (var type in svc.Types.Where(t =>
            t.Layer is ArchitectureLayer.Domain or ArchitectureLayer.Application))
        {
            if (type.Attributes.Contains("HasStaticState"))
                violations.Add(new RuleViolation
                {
                    RuleId      = RuleId,
                    Severity    = RuleSeverity.Error,
                    ProjectName = svc.ProjectName,
                    Message     = $"Type '{type.ShortName}' in [{type.Layer}] has static mutable state. " +
                                  $"This leaks data across tenants in multi-tenant deployments. " +
                                  $"Use instance fields or IMemoryCache/IDistributedCache instead.",
                    Subject     = type,
                });
        }

        return new RuleResult { RuleId = RuleId, RuleName = RuleName, Category = Category, Version = Version, Violations = violations };
    }
}



