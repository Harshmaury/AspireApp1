namespace Aegis.Core.Rules;

using Aegis.Core.Model;

public sealed class RegionAffinityRule : IRule
{
    public string       RuleId   => "AGS-015";
    public string       RuleName => "Region Write Affinity";
    public RuleCategory Category => RuleCategory.Boundary;
    public string       Version  => "1.0";

    public RuleResult Evaluate(ArchitectureModel model)
    {
        var violations = new List<RuleViolation>();

        foreach (var svc in model.Services)
        {
            CheckReadWriteSplit(svc, violations);
            CheckConsumerWriteBack(svc, violations);
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

    private static void CheckReadWriteSplit(ServiceModel svc, List<RuleViolation> violations)
    {
        var dbContexts = svc.Types
            .Where(t => t.Kind == NodeKind.DbContext)
            .ToList();

        if (dbContexts.Count == 0) return;

        foreach (var ctx in dbContexts)
        {
            var hasReadOnly = svc.DiRegistrations
                .Any(d => d.ServiceType.Contains("ReadOnly") ||
                          d.ImplementationType.Contains("ReadOnly"));

            if (!hasReadOnly)
            {
                violations.Add(new RuleViolation
                {
                    RuleId      = "AGS-015",
                    Severity    = RuleSeverity.Warning,
                    ProjectName = svc.ProjectName,
                    Message     = $"{ctx.ShortName}: No read-only DbContext variant detected. " +
                                  $"Register both 'ServiceDb' and 'ServiceDbReadOnly' connection " +
                                  $"strings to enable SECONDARY region read routing.",
                    Subject     = ctx,
                });
            }
        }
    }

    private static void CheckConsumerWriteBack(ServiceModel svc, List<RuleViolation> violations)
    {
        foreach (var consumer in svc.KafkaConsumers)
        {
            var consumerType = svc.Types
                .FirstOrDefault(t => t.ShortName == consumer.ConsumerClass);
            if (consumerType == null) continue;

            var directDbContext = svc.Edges
                .Where(e => e.From.ShortName == consumer.ConsumerClass &&
                            e.Kind == EdgeKind.ConstructorInjection)
                .FirstOrDefault(e => e.ToFullName.Contains("DbContext") &&
                                     !e.ToFullName.Contains("ReadOnly"));

            if (directDbContext != null)
            {
                violations.Add(new RuleViolation
                {
                    RuleId      = "AGS-015",
                    Severity    = RuleSeverity.Warning,
                    ProjectName = svc.ProjectName,
                    Message     = $"{consumer.ConsumerClass}: Kafka consumer directly injects " +
                                  $"'{directDbContext.ToFullName}' (RW DbContext). " +
                                  $"Consumers must not write in SECONDARY regions. " +
                                  $"Use IReadOnlyRepository or dispatch via IMediator instead.",
                    Subject     = consumerType,
                });
            }
        }
    }
}