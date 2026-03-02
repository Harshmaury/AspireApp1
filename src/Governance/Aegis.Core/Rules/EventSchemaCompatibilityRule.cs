namespace Aegis.Core.Rules;

using Aegis.Core.Model;
using System.Text.Json;

public sealed class EventSchemaCompatibilityRule : IRule
{
    private readonly string _schemaDir;

    public EventSchemaCompatibilityRule(string schemaDir) => _schemaDir = schemaDir;

    public string       RuleId   => "AGS-011";
    public string       RuleName => "Event Schema Compatibility";
    public RuleCategory Category => RuleCategory.Contract;
    public string       Version  => "1.0";

    public RuleResult Evaluate(ArchitectureModel model)
    {
        var violations = new List<RuleViolation>();

        if (!Directory.Exists(_schemaDir))
        {
            violations.Add(new RuleViolation
            {
                RuleId   = RuleId,
                Severity = RuleSeverity.Info,
                Message  = $"Schema directory '{_schemaDir}' not found. Run 'ums snapshot create' first.",
            });
            return new RuleResult { RuleId = RuleId, RuleName = RuleName, Category = Category, Version = Version, Violations = violations };
        }

        foreach (var svc in model.Services)
        foreach (var type in svc.Types.Where(t => t.Kind is NodeKind.DomainEvent or NodeKind.IntegrationEvent))
        {
            var schemaFile = Path.Combine(_schemaDir, $"{type.FullName}.json");
            if (!File.Exists(schemaFile)) continue;

            var baseline = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(schemaFile)) ?? [];
            var current  = type.Methods.Select(m => $"{m.Name}:{m.ReturnType}").ToHashSet();
            var removed  = baseline.Where(f => !current.Contains(f)).ToList();

            foreach (var field in removed)
            {
                violations.Add(new RuleViolation
                {
                    RuleId      = RuleId,
                    Severity    = RuleSeverity.Error,
                    ProjectName = svc.ProjectName,
                    Message     = $"Event '{type.ShortName}' removed field '{field}'. Breaking schema change.",
                    Subject     = type,
                });
            }
        }

        return new RuleResult { RuleId = RuleId, RuleName = RuleName, Category = Category, Version = Version, Violations = violations };
    }
}