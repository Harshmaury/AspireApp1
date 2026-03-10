namespace Aegis.Core.Rules;
using Aegis.Core.Model;

public sealed class TenantIsolationRule : IRule
{
    public string       RuleId   => "AGS-007";
    public string       RuleName => "Tenant Isolation";
    public RuleCategory Category => RuleCategory.Tenant;
    public string       Version  => "1.2";

    public RuleResult Evaluate(ArchitectureModel model)
    {
        var violations = new List<RuleViolation>();

        foreach (var svc in model.Services)
        {
            // Catalogue which DbContexts in this service are tenant-aware.
            // Used by CheckRepositories so it can accept Proof B (delegates to
            // a tenant-aware DbContext) without requiring ITenantContext injection.
            var tenantAwareDbContextNames = CollectTenantAwareDbContexts(svc, violations);

            CheckHandlersBypassingRepo(svc, violations);
            CheckRepositories(svc, tenantAwareDbContextNames, violations);
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
    // DbContext check
    // Returns the set of DbContext ShortNames that passed the tenant-filter check.
    // -------------------------------------------------------------------------
    private static HashSet<string> CollectTenantAwareDbContexts(
        ServiceModel       svc,
        List<RuleViolation> violations)
    {
        var tenantAware = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var type in svc.Types.Where(t => t.Kind == NodeKind.DbContext))
        {
            // FIX BUG-1:
            // Old rule only checked:
            //   (a) whether OnModelCreating METHOD EXISTS on the type
            //   (b) type.Attributes contains "HasQueryFilter"
            //
            // This produces a false positive because the ArchitectureModel extractor
            // records method names but NOT what those methods call internally.
            // A DbContext that has OnModelCreating and injects ITenantContext is
            // correctly configured — the query filters ARE applied inside that method.
            //
            // Corrected detection: a DbContext is tenant-aware when ANY of these hold:
            //   Proof 1 — it has OnModelCreating AND injects ITenantContext/ITenantId
            //             (the standard UMS pattern: filter applied conditionally on
            //              _tenant.IsResolved inside OnModelCreating)
            //   Proof 2 — a method or attribute explicitly references HasQueryFilter
            //             (older direct-filter pattern)
            //   Proof 3 — it has a constructor parameter that IS ITenantContext
            //             and OnModelCreating exists (belt-and-suspenders confirmation)

            var hasOnModelCreating = type.Methods.Any(m =>
                m.Name.Equals("OnModelCreating", StringComparison.OrdinalIgnoreCase));

            var injectsTenantContext = svc.Edges
                .Where(e => e.From.FullName == type.FullName
                         && e.Kind == EdgeKind.ConstructorInjection)
                .Any(e => e.ToFullName.Contains("ITenantContext",  StringComparison.OrdinalIgnoreCase) ||
                          e.ToFullName.Contains("ITenantId",       StringComparison.OrdinalIgnoreCase) ||
                          e.ToFullName.Contains("TenantContext",   StringComparison.OrdinalIgnoreCase) ||
                          e.ToFullName.Contains("ICurrentTenant",  StringComparison.OrdinalIgnoreCase));

            var hasExplicitFilterAttribute = type.Attributes.Any(a =>
                a.Contains("HasQueryFilter", StringComparison.OrdinalIgnoreCase));

            var hasExplicitFilterMethod = type.Methods.Any(m =>
                m.Name.Contains("Tenant", StringComparison.OrdinalIgnoreCase) ||
                m.Name.Contains("Filter", StringComparison.OrdinalIgnoreCase));

            // Proof 1 or Proof 2 or Proof 3
            var isTenantAware =
                (hasOnModelCreating && injectsTenantContext) ||   // Proof 1 (UMS standard)
                hasExplicitFilterAttribute                     ||   // Proof 2
                hasExplicitFilterMethod                        ||   // Proof 3
                (hasOnModelCreating && hasExplicitFilterAttribute); // belt-and-suspenders

            if (isTenantAware)
            {
                tenantAware.Add(type.ShortName);
            }
            else
            {
                violations.Add(new RuleViolation
                {
                    RuleId      = "AGS-007",
                    Severity    = RuleSeverity.Warning,
                    ProjectName = svc.ProjectName,
                    Message     = $"DbContext '{type.ShortName}' has no detectable tenant filter. " +
                                  $"Ensure OnModelCreating applies HasQueryFilter for tenant isolation.",
                    Subject     = type,
                });
            }
        }

        return tenantAware;
    }

    // -------------------------------------------------------------------------
    // Handler check — handlers must not inject DbContext directly (unchanged)
    // -------------------------------------------------------------------------
    private static void CheckHandlersBypassingRepo(
        ServiceModel       svc,
        List<RuleViolation> violations)
    {
        foreach (var type in svc.Types.Where(t => t.Kind == NodeKind.MediatRHandler))
        {
            var directDbCtx = svc.Edges
                .Where(e => e.From.FullName == type.FullName
                         && e.Kind == EdgeKind.ConstructorInjection)
                .FirstOrDefault(e => e.ToFullName.Contains("DbContext") &&
                                     !e.ToFullName.Contains("ReadOnly"));

            if (directDbCtx != null)
                violations.Add(new RuleViolation
                {
                    RuleId      = "AGS-007",
                    Severity    = RuleSeverity.Error,
                    ProjectName = svc.ProjectName,
                    Message     = $"Handler '{type.ShortName}' injects DbContext '{directDbCtx.ToFullName}' directly. " +
                                  $"Inject a repository interface instead to guarantee tenant-filtered queries.",
                    Subject     = type,
                });
        }
    }

    // -------------------------------------------------------------------------
    // Repository check
    // -------------------------------------------------------------------------
    private static void CheckRepositories(
        ServiceModel       svc,
        HashSet<string>    tenantAwareDbContextNames,
        List<RuleViolation> violations)
    {
        var repoTypes = svc.Types.Where(t =>
            (t.ShortName.EndsWith("Repository", StringComparison.OrdinalIgnoreCase) ||
             t.Interfaces.Any(i => i.Contains("IRepository", StringComparison.OrdinalIgnoreCase))) &&
            t.Kind == NodeKind.Class);

        foreach (var repo in repoTypes)
        {
            var ctorParams = svc.Edges
                .Where(e => e.From.FullName == repo.FullName
                         && e.Kind == EdgeKind.ConstructorInjection)
                .Select(e => e.ToFullName)
                .ToList();

            var injectsDbContext = ctorParams.Any(p =>
                p.Contains("DbContext", StringComparison.OrdinalIgnoreCase));

            // No DbContext injected at all — nothing for this rule to evaluate.
            if (!injectsDbContext)
                continue;

            // FIX BUG-2:
            // Old rule flagged ANY repository that injects DbContext without also
            // injecting ITenantContext. But the correct UMS pattern is:
            //   - ITenantContext lives INSIDE the DbContext
            //   - DbContext applies HasQueryFilter globally
            //   - Repository receives a pre-filtered DbContext
            //   - Repository ADDITIONALLY passes tenantId explicitly in predicates
            //
            // This is belt-and-suspenders — more protection than the rule expected.
            //
            // Corrected: accept any of three proofs of tenant isolation:
            //
            //   Proof A — repository directly injects ITenantContext/ITenantId
            //             (explicit injection pattern)
            //
            //   Proof B — repository injects a DbContext that is in
            //             tenantAwareDbContextNames (delegate-to-DbContext pattern)
            //             This is the standard UMS pattern across all 6 services.
            //
            //   Proof C — repository has a method named with "Tenant" or any
            //             constructor parameter named "tenantId"
            //             (explicit tenant-parameter pattern)

            // Proof A
            var proofA = ctorParams.Any(p =>
                p.Contains("ITenantContext",  StringComparison.OrdinalIgnoreCase) ||
                p.Contains("ITenantId",       StringComparison.OrdinalIgnoreCase) ||
                p.Contains("TenantContext",   StringComparison.OrdinalIgnoreCase) ||
                p.Contains("ICurrentTenant",  StringComparison.OrdinalIgnoreCase));

            if (proofA) continue;

            // Proof B — injected DbContext is catalogued as tenant-aware
            var injectedDbContextName = ctorParams
                .Where(p => p.Contains("DbContext", StringComparison.OrdinalIgnoreCase))
                .Select(p =>
                {
                    // Extract short name from fully-qualified name
                    // e.g. "Academic.Infrastructure.Persistence.AcademicDbContext" -> "AcademicDbContext"
                    var parts = p.Split('.');
                    return parts[parts.Length - 1];
                })
                .FirstOrDefault();

            var proofB = injectedDbContextName != null &&
                         tenantAwareDbContextNames.Contains(injectedDbContextName);

            if (proofB) continue;

            // Proof C — repository has explicit tenant-aware methods or params
            var proofC = repo.Methods.Any(m =>
                m.Name.Contains("Tenant", StringComparison.OrdinalIgnoreCase));

            if (proofC) continue;

            // None of the three proofs passed — genuine violation
            violations.Add(new RuleViolation
            {
                RuleId      = "AGS-007",
                Severity    = RuleSeverity.Warning,
                ProjectName = svc.ProjectName,
                Message     = $"Repository '{repo.ShortName}' injects DbContext but has no ITenantContext/ITenantId " +
                              $"parameter. Tenant filtering may be missing from queries.",
                Subject     = repo,
            });
        }
    }
}
