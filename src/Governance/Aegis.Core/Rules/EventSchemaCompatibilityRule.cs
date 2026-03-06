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
    public string       Version  => "1.1";

    public RuleResult Evaluate(ArchitectureModel model)
    {
        var violations = new List<RuleViolation>();

        if (!Directory.Exists(_schemaDir))
        {
            violations.Add(new RuleViolation
            {
                RuleId   = RuleId, Severity = RuleSeverity.Info,
                Message  = $"Schema directory '{_schemaDir}' not found. Run 'ums govern snapshot create' first.",
            });
            return new RuleResult { RuleId = RuleId, RuleName = RuleName, Category = Category, Version = Version, Violations = violations };
        }

        foreach (var svc in model.Services)
        foreach (var type in svc.Types.Where(t => t.Kind is NodeKind.DomainEvent or NodeKind.IntegrationEvent))
        {
            var schemaFile = Path.Combine(_schemaDir, $"{type.FullName}.json");
            if (!File.Exists(schemaFile)) continue;

            var baseline = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(schemaFile)) ?? [];
            var baseSet  = baseline.ToHashSet();
            var current  = type.Methods.Select(m => $"{m.Name}:{m.ReturnType}").ToList();
            var currSet  = current.ToHashSet();

            // BREAKING: removed fields â€” consumers will fail deserializing required fields
            foreach (var field in baseline.Where(f => !currSet.Contains(f)))
                violations.Add(new RuleViolation
                {
                    RuleId      = RuleId, Severity = RuleSeverity.Error,
                    ProjectName = svc.ProjectName, Subject = type,
                    Message     = $"[REMOVED] Event '{type.ShortName}' removed field '{field}'. " +
                                  $"Breaking change â€” existing consumers will fail.",
                });

            // BREAKING: added non-nullable fields â€” old producers won't populate them
            foreach (var field in current.Where(f => !baseSet.Contains(f)))
            {
                var fieldType = field.Split(':').ElementAtOrDefault(1) ?? "";
                var isNullable = fieldType.EndsWith("?") ||
                                 fieldType.StartsWith("System.Nullable") ||
                                 fieldType == "string";          // strings are implicitly nullable in JSON

                violations.Add(new RuleViolation
                {
                    RuleId      = RuleId,
                    Severity    = isNullable ? RuleSeverity.Warning : RuleSeverity.Error,
                    ProjectName = svc.ProjectName,
                    Subject     = type,
                    Message     = isNullable
                        ? $"[ADDED-NULLABLE] Event '{type.ShortName}' added nullable field '{field}'. " +
                          $"Old producers will send null â€” ensure consumers handle missing values."
                        : $"[ADDED-REQUIRED] Event '{type.ShortName}' added non-nullable field '{field}'. " +
                          $"Breaking change â€” old producers cannot populate this field.",
                });
            }

            // BREAKING: field type changes
            var baseFieldMap = baseline
                .Select(f => f.Split(':', 2))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0], p => p[1]);
            foreach (var m in type.Methods)
            {
                if (baseFieldMap.TryGetValue(m.Name, out var oldType) && oldType != m.ReturnType)
                    violations.Add(new RuleViolation
                    {
                        RuleId      = RuleId, Severity = RuleSeverity.Error,
                        ProjectName = svc.ProjectName, Subject = type,
                        Message     = $"[TYPE-CHANGED] Event '{type.ShortName}' field '{m.Name}' " +
                                      $"changed type from '{oldType}' to '{m.ReturnType}'. Breaking change.",
                    });
            }
        }

        return new RuleResult { RuleId = RuleId, RuleName = RuleName, Category = Category, Version = Version, Violations = violations };
    }
}



