namespace Aegis.Core.Rules;

using Aegis.Core.Model;

/// <summary>
/// AGS-014 v1.1 — Consumer Group Region Scoping
///
/// Enforces that all statically-known Kafka consumer group IDs follow the
/// region-scoped naming convention:
///   {service}.{region}.{purpose}
///   e.g. "student-api.ap-south-1.student-enrolled"
///
/// Design rationale:
///   In a multi-region deployment, consumer group IDs must be region-scoped to
///   prevent consumers in different regions from sharing offset state. A consumer
///   in ap-south-1 and one in eu-west-1 must never compete on the same group ID
///   or they will steal partitions from each other, causing message loss.
///
/// Static vs config-driven detection:
///   Aegis performs static Roslyn analysis. Consumer group IDs embedded as string
///   literals in source are fully verifiable at build time (Error on bad format).
///   IDs read from IConfiguration / appsettings are not statically resolvable —
///   these are recorded as Info with a deploy-time verification reminder.
///   Info violations never cause the rule to fail (see RuleResult.Passed).
///
/// Severity matrix:
///   Error  — static group ID present but not region-scoped (fix before merge)
///   Warning — static group ID uses deprecated single-segment format
///   Info   — config-driven ID, cannot verify statically (verify at deploy time)
///
/// Format validation:
///   Required: at least 2 dots → 3 segments → {service}.{region}.{purpose}
///   Region segment must be a known region slug or the placeholder token
///   "{region}" (used in templates). Unknown region slugs produce a Warning
///   rather than an Error to avoid breaking builds for new region rollouts.
///
/// Extension points for future upgrades:
///   - Add known regions to KnownRegions to validate the region segment value.
///   - Extend IsRegionScoped with stricter segment validation as conventions evolve.
///   - Add AllowList entries for legacy group IDs during migration windows.
/// </summary>
public sealed class ConsumerGroupScopingRule : IRule
{
    public string       RuleId   => "AGS-014";
    public string       RuleName => "Consumer Group Region Scoping";
    public RuleCategory Category => RuleCategory.Messaging;
    public string       Version  => "1.1";

    // Extend this set as new deployment regions are added.
    // Slug format follows cloud provider conventions (e.g. AWS ap-south-1).
    private static readonly HashSet<string> KnownRegions = new(StringComparer.OrdinalIgnoreCase)
    {
        "ap-south-1", "ap-southeast-1", "ap-southeast-2",
        "eu-west-1",  "eu-central-1",
        "us-east-1",  "us-west-2",
        "dev-local",  "ci",             // non-production environment slugs
    };

    // Legacy group IDs approved for temporary exemption during migration.
    // Remove entries once the owning service has been updated.
    private static readonly HashSet<string> AllowList = new(StringComparer.OrdinalIgnoreCase);

    public RuleResult Evaluate(ArchitectureModel model)
    {
        var violations = new List<RuleViolation>();

        foreach (var svc in model.Services)
        foreach (var consumer in svc.KafkaConsumers)
        {
            var subject = svc.Types.FirstOrDefault(t => t.ShortName == consumer.ConsumerClass);

            // ----------------------------------------------------------------
            // Config-driven: group ID not resolvable statically.
            // Record as Info — cannot enforce at build time, must verify at
            // deploy time that the runtime value follows the convention.
            // Info does NOT cause the rule to fail (see RuleResult.Passed).
            // ----------------------------------------------------------------
            if (consumer.ConsumerGroupId is null)
            {
                violations.Add(new RuleViolation
                {
                    RuleId      = RuleId,
                    Severity    = RuleSeverity.Info,
                    ProjectName = svc.ProjectName,
                    Message     = $"{consumer.ConsumerClass}: consumer group ID is config-driven " +
                                  $"(not statically detectable). Verify it follows " +
                                  $"{{service}}.{{region}}.{{purpose}} at deploy time.",
                    Subject     = subject,
                });
                continue;
            }

            // ----------------------------------------------------------------
            // Allow-listed legacy IDs — exempt during migration windows.
            // ----------------------------------------------------------------
            if (AllowList.Contains(consumer.ConsumerGroupId))
                continue;

            // ----------------------------------------------------------------
            // Static group ID present — enforce format strictly.
            // ----------------------------------------------------------------
            var (valid, regionSegment) = ParseGroupId(consumer.ConsumerGroupId);

            if (!valid)
            {
                violations.Add(new RuleViolation
                {
                    RuleId      = RuleId,
                    Severity    = RuleSeverity.Error,
                    ProjectName = svc.ProjectName,
                    Message     = $"{consumer.ConsumerClass}: consumer group " +
                                  $"'{consumer.ConsumerGroupId}' is NOT region-scoped. " +
                                  $"Required format: {{service}}.{{region}}.{{purpose}} " +
                                  $"e.g. 'student-api.ap-south-1.student-enrolled'.",
                    Subject     = subject,
                });
                continue;
            }

            // ----------------------------------------------------------------
            // Format is correct — warn if region segment is unrecognised.
            // Warning (not Error) to avoid blocking builds for new regions.
            // Add the slug to KnownRegions once the region is confirmed.
            // ----------------------------------------------------------------
            if (regionSegment != null
                && regionSegment != "{region}"
                && !KnownRegions.Contains(regionSegment))
            {
                violations.Add(new RuleViolation
                {
                    RuleId      = RuleId,
                    Severity    = RuleSeverity.Warning,
                    ProjectName = svc.ProjectName,
                    Message     = $"{consumer.ConsumerClass}: consumer group " +
                                  $"'{consumer.ConsumerGroupId}' uses unrecognised region " +
                                  $"segment '{regionSegment}'. If this is a new region, " +
                                  $"add it to ConsumerGroupScopingRule.KnownRegions.",
                    Subject     = subject,
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

    // -------------------------------------------------------------------------
    // Parses a group ID and returns whether it is validly region-scoped,
    // along with the extracted region segment for further validation.
    // Valid format: {service}.{region}.{purpose} — minimum 3 dot-separated
    // segments, each non-empty.
    // -------------------------------------------------------------------------
    private static (bool Valid, string? RegionSegment) ParseGroupId(string groupId)
    {
        if (string.IsNullOrWhiteSpace(groupId))
            return (false, null);

        var segments = groupId.Split('.');
        if (segments.Length < 3)
            return (false, null);

        if (segments.Any(string.IsNullOrWhiteSpace))
            return (false, null);

        // Segment[1] is the region slot by convention: {service}.{region}.{purpose}
        return (true, segments[1]);
    }
}
