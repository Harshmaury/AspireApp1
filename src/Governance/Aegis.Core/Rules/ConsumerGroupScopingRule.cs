namespace Aegis.Core.Rules;

using Aegis.Core.Model;

/// AGS-014 — ConsumerGroupScopingRule
/// Enforces that all Kafka consumer group IDs are region-scoped:
///   {service}.{region}.{purpose}
public sealed class ConsumerGroupScopingRule : IRule
{
    public string       RuleId   => "AGS-014";
    public string       RuleName => "Consumer Group Region Scoping";
    public RuleCategory Category => RuleCategory.Messaging;
    public string       Version  => "1.0";

    private static bool IsRegionScoped(string groupId) =>
        groupId.Count(c => c == '.') >= 2;

    public RuleResult Evaluate(ArchitectureModel model)
    {
        var violations = new List<RuleViolation>();

        foreach (var svc in model.Services)
        foreach (var consumer in svc.KafkaConsumers)
        {
            if (consumer.ConsumerGroupId == null)
            {
                violations.Add(new RuleViolation
                {
                    RuleId      = RuleId,
                    Severity    = RuleSeverity.Info,
                    ProjectName = svc.ProjectName,
                    Message     = $"{consumer.ConsumerClass}: consumer group ID is config-driven " +
                                  $"(not statically detectable). Verify it follows " +
                                  $"{{service}}.{{region}}.{{purpose}} at deploy time.",
                    Subject     = svc.Types.FirstOrDefault(t => t.ShortName == consumer.ConsumerClass),
                });
                continue;
            }

            if (!IsRegionScoped(consumer.ConsumerGroupId))
            {
                violations.Add(new RuleViolation
                {
                    RuleId      = RuleId,
                    Severity    = RuleSeverity.Error,
                    ProjectName = svc.ProjectName,
                    Message     = $"{consumer.ConsumerClass}: consumer group '{consumer.ConsumerGroupId}' " +
                                  $"is NOT region-scoped. Required format: " +
                                  $"{{service}}.{{region}}.{{purpose}} " +
                                  $"e.g. 'student-api.dev-local.student-enrolled'",
                    Subject     = svc.Types.FirstOrDefault(t => t.ShortName == consumer.ConsumerClass),
                });
            }
        }

        return new RuleResult
        {
            RuleId     = RuleId,
            RuleName   = RuleName,
            Category   = Category,
            Version    = Version,
            Violations = violations,
        };
    }
}