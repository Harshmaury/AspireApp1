namespace Aegis.Core.Rules;

using Aegis.Core.Model;

/// <summary>
/// AGS-017 — Outbox Publishing Enforcement
///
/// Handlers must not call Kafka producer methods (Produce, ProduceAsync,
/// PublishAsync, SendAsync) directly. Domain events must be persisted
/// to OutboxMessage and published by OutboxRelayService.
///
/// Rationale:
///   All 9 services use an Outbox relay pattern. A handler that bypasses
///   the outbox loses atomicity — the event may be published even if the
///   database transaction rolls back, causing phantom events downstream.
///
/// Detection strategy:
///   KafkaExtractor already captures which classes call producer methods
///   (via ServiceModel.KafkaProducers). We flag any producer class that
///   is identified as a handler: NodeKind.MediatRHandler, name ending in
///   "Handler", or implementing IRequestHandler.
///
/// Exemptions:
///   OutboxRelayService is the legitimate producer — it is explicitly
///   excluded. Add other infrastructure relay classes to ExemptClasses
///   if your architecture introduces additional relay patterns.
///
/// Severity: Error — bypassing the outbox is a correctness/atomicity bug.
/// </summary>
public sealed class OutboxPublishingRule : IRule
{
    public string       RuleId   => "AGS-017";
    public string       RuleName => "Outbox Publishing Enforcement";
    public RuleCategory Category => RuleCategory.Messaging;
    public string       Version  => "1.0";

    /// <summary>
    /// Classes that are legitimately allowed to produce directly to Kafka.
    /// These are the relay/infrastructure classes — NOT application handlers.
    /// </summary>
    private static readonly HashSet<string> ExemptClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "OutboxRelayService",
        "KafkaOutboxRelay",
        "OutboxWorker",
        "OutboxProcessor",
        "IntegrationEventPublisher",   // common relay alias
    };

    public RuleResult Evaluate(ArchitectureModel model)
    {
        var violations = new List<RuleViolation>();

        foreach (var svc in model.Services)
        {
            // Build a fast-lookup: ShortName → TypeNode for this service
            var typeByName = svc.Types.ToDictionary(
                t => t.ShortName,
                t => t,
                StringComparer.Ordinal);

            foreach (var prod in svc.KafkaProducers)
            {
                // Skip legitimate relay classes
                if (ExemptClasses.Contains(prod.ProducerClass)) continue;

                // Determine if the producing class is a handler
                var isHandler = false;
                string? layer = null;

                if (typeByName.TryGetValue(prod.ProducerClass, out var typeNode))
                {
                    isHandler =
                        typeNode.Kind == NodeKind.MediatRHandler ||
                        typeNode.ShortName.EndsWith("Handler",  StringComparison.Ordinal) ||
                        typeNode.Interfaces.Any(i =>
                            i.Contains("IRequestHandler", StringComparison.Ordinal) ||
                            i.Contains("INotificationHandler", StringComparison.Ordinal));

                    layer = typeNode.Layer.ToString();
                }
                else
                {
                    // TypeNode not found (could be in a different scan boundary).
                    // Fall back to name heuristic only.
                    isHandler = prod.ProducerClass.EndsWith("Handler", StringComparison.Ordinal) ||
                                prod.ProducerClass.EndsWith("Consumer", StringComparison.Ordinal);
                }

                if (!isHandler) continue;

                violations.Add(new RuleViolation
                {
                    RuleId      = RuleId,
                    Severity    = RuleSeverity.Error,
                    ProjectName = svc.ProjectName,
                    Message     = $"Handler '{prod.ProducerClass}' in service '{svc.ProjectName}' " +
                                  $"directly produces event '{prod.EventTypeName}' to Kafka" +
                                  (layer != null ? $" (layer: {layer})" : string.Empty) +
                                  $". Handlers must persist to OutboxMessage and let OutboxRelayService publish. " +
                                  $"Direct Kafka production bypasses atomicity guarantees.",
                    Subject     = typeByName.TryGetValue(prod.ProducerClass, out var tn) ? tn : null,
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
