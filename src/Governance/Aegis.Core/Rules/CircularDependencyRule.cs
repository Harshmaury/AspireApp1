namespace Aegis.Core.Rules;
using Aegis.Core.Model;

public sealed class CircularDependencyRule : IRule
{
    public string       RuleId   => "AGS-005";
    public string       RuleName => "Circular Dependency Detection";
    public RuleCategory Category => RuleCategory.Dependency;
    public string       Version  => "1.1";

    public RuleResult Evaluate(ArchitectureModel model)
    {
        var violations = new List<RuleViolation>();

        foreach (var svc in model.Services)
        {
            var graph = svc.Edges
                .GroupBy(e => e.From.FullName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ToFullName).ToHashSet());

            // WHITE=unvisited, GRAY=in-stack, BLACK=done
            var color    = new Dictionary<string, int>(StringComparer.Ordinal); // 0=W,1=G,2=B
            var reported = new HashSet<string>();

            foreach (var type in svc.Types)
                if (!color.TryGetValue(type.FullName, out var c) || c == 0)
                    Dfs(type.FullName, graph, color, new List<string>(),
                        reported, violations, svc, RuleId);
        }

        return new RuleResult { RuleId = RuleId, RuleName = RuleName, Category = Category, Version = Version, Violations = violations };
    }

    private static void Dfs(
        string node,
        Dictionary<string, HashSet<string>> graph,
        Dictionary<string, int> color,
        List<string> stack,
        HashSet<string> reported,
        List<RuleViolation> violations,
        ServiceModel svc, string ruleId)
    {
        color[node] = 1; // GRAY â€” in current DFS path
        stack.Add(node);

        if (graph.TryGetValue(node, out var neighbors))
        {
            foreach (var n in neighbors)
            {
                color.TryGetValue(n, out var nc);
                if (nc == 2) continue; // BLACK â€” fully processed, safe

                if (nc == 1) // GRAY â€” back-edge = cycle found
                {
                    var loopStart = stack.IndexOf(n);
                    var cycle     = stack.Skip(loopStart).ToList();
                    var key       = string.Join("->", cycle.OrderBy(x => x));
                    if (reported.Add(key))
                    {
                        var path    = string.Join(" -> ", cycle) + " -> " + n;
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
                    continue;
                }

                Dfs(n, graph, color, stack, reported, violations, svc, ruleId);
            }
        }

        stack.RemoveAt(stack.Count - 1);
        color[node] = 2; // BLACK â€” done
    }
}



