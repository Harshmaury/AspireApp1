namespace Aegis.Core.Rules;

using Aegis.Core.Model;

public sealed class CircularDependencyRule : IRule
{
    public string       RuleId   => "AGS-005";
    public string       RuleName => "Circular Dependency Detection";
    public RuleCategory Category => RuleCategory.Dependency;
    public string       Version  => "1.0";

    public RuleResult Evaluate(ArchitectureModel model)
    {
        var violations = new List<RuleViolation>();

        foreach (var svc in model.Services)
        {
            var graph = svc.Edges
                .GroupBy(e => e.From.FullName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ToFullName).ToHashSet());

            foreach (var type in svc.Types)
            {
                var visited = new HashSet<string>();
                if (HasCycle(type.FullName, graph, visited, new HashSet<string>()))
                {
                    violations.Add(new RuleViolation
                    {
                        RuleId      = RuleId,
                        Severity    = RuleSeverity.Error,
                        ProjectName = svc.ProjectName,
                        Message     = $"Circular dependency detected involving '{type.ShortName}'.",
                        Subject     = type,
                    });
                }
            }
        }

        return new RuleResult { RuleId = RuleId, RuleName = RuleName, Category = Category, Version = Version, Violations = violations };
    }

    private static bool HasCycle(string node, Dictionary<string, HashSet<string>> graph, HashSet<string> visited, HashSet<string> stack)
    {
        if (stack.Contains(node)) return true;
        if (visited.Contains(node)) return false;
        visited.Add(node); stack.Add(node);
        if (graph.TryGetValue(node, out var neighbors))
            foreach (var n in neighbors)
                if (HasCycle(n, graph, visited, stack)) return true;
        stack.Remove(node);
        return false;
    }
}