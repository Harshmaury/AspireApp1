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

            var visited    = new HashSet<string>();
            var reported   = new HashSet<string>();   // dedup: canonical cycle key

            foreach (var type in svc.Types)
            {
                var stack = new List<string>();
                FindCycle(type.FullName, graph, visited, stack, reported, violations, svc, RuleId);
            }
        }

        return new RuleResult { RuleId = RuleId, RuleName = RuleName, Category = Category, Version = Version, Violations = violations };
    }

    private static void FindCycle(
        string node,
        Dictionary<string, HashSet<string>> graph,
        HashSet<string> visited,
        List<string> stack,
        HashSet<string> reported,
        List<RuleViolation> violations,
        ServiceModel svc,
        string ruleId)
    {
        if (visited.Contains(node)) return;

        int loopStart = stack.IndexOf(node);
        if (loopStart >= 0)
        {
            // Extract the cycle segment and canonicalise so A->B->A and B->A->B are the same
            var cycle     = stack.Skip(loopStart).ToList();
            var cycleKey  = string.Join("->", cycle.OrderBy(x => x));
            if (reported.Add(cycleKey))
            {
                var path    = string.Join(" -> ", cycle) + " -> " + node;
                var subject = svc.Types.FirstOrDefault(t => t.FullName == cycle[loopStart]);
                violations.Add(new RuleViolation
                {
                    RuleId      = ruleId,
                    Severity    = RuleSeverity.Error,
                    ProjectName = svc.ProjectName,
                    Message     = $"Circular dependency: {path}",
                    Subject     = subject,
                });
            }
            return;
        }

        stack.Add(node);
        if (graph.TryGetValue(node, out var neighbors))
            foreach (var n in neighbors)
                FindCycle(n, graph, visited, stack, reported, violations, svc, ruleId);

        stack.RemoveAt(stack.Count - 1);
        visited.Add(node);
    }
}
