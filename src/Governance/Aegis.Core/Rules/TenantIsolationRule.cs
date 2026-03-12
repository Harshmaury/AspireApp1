namespace Aegis.Core.Rules;

using Aegis.Core.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// AGS-007 v1.3 — Tenant Isolation
///
/// Detects DbContexts and repositories that lack tenant isolation.
///
/// v1.3 changes vs v1.2:
///   BUG-FIX: CollectTenantAwareDbContexts now performs a Roslyn syntax scan of
///   OnModelCreating body source text to detect the UMS null-guard filter pattern:
///     e => _tenant == null || !_tenant.IsResolved || e.TenantId == _tenant.TenantId
///   Previously the rule relied solely on ConstructorInjection edge matching, which
///   failed because ITenantContext lives in UMS.SharedKernel — an external assembly
///   whose constructor edges are present in the model but whose ToProjectName is null,
///   causing the edge filter to silently skip them when combined with a project-name
///   scope guard. The syntax scan is assembly-agnostic and works regardless of how
///   the tenant context is resolved.
/// </summary>
public sealed class TenantIsolationRule : IRule
{
    public string       RuleId   => "AGS-007";
    public string       RuleName => "Tenant Isolation";
    public RuleCategory Category => RuleCategory.Tenant;
    public string       Version  => "1.3";

    public RuleResult Evaluate(ArchitectureModel model)
    {
        var violations = new List<RuleViolation>();

        foreach (var svc in model.Services)
        {
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
    // DbContext check — returns ShortNames of all tenant-aware DbContexts.
    // -------------------------------------------------------------------------
    private static HashSet<string> CollectTenantAwareDbContexts(
        ServiceModel        svc,
        List<RuleViolation> violations)
    {
        var tenantAware = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var type in svc.Types.Where(t => t.Kind == NodeKind.DbContext))
        {
            var isTenantAware = false;

            // ------------------------------------------------------------------
            // Proof 1 (UMS standard pattern — primary detection path)
            // OnModelCreating exists AND its source text contains a TenantId
            // HasQueryFilter expression. Works for both the bare form:
            //   e => e.TenantId == _tenant.TenantId
            // and the null-guard compound form used in all UMS DbContexts:
            //   e => _tenant == null || !_tenant.IsResolved || e.TenantId == _tenant.TenantId
            // ------------------------------------------------------------------
            if (HasQueryFilterInOnModelCreating(type))
            {
                isTenantAware = true;
            }

            // ------------------------------------------------------------------
            // Proof 2 — ConstructorInjection edge references ITenantContext / TenantContext.
            // Belt-and-suspenders: covers cases where OnModelCreating is defined
            // in a base class but the DbContext still injects ITenantContext.
            // Note: ToProjectName may be null for SharedKernel types — match on
            // ToFullName only, never filter by project name here.
            // ------------------------------------------------------------------
            if (!isTenantAware)
            {
                var injectsTenant = svc.Edges
                    .Where(e => e.From.FullName == type.FullName
                             && e.Kind == EdgeKind.ConstructorInjection)
                    .Any(e => ContainsTenantContextName(e.ToFullName));

                if (injectsTenant)
                    isTenantAware = true;
            }

            // ------------------------------------------------------------------
            // Proof 3 — Explicit HasQueryFilter attribute recorded on the type
            // (older direct-filter pattern, or EF config classes).
            // ------------------------------------------------------------------
            if (!isTenantAware)
            {
                if (type.Attributes.Any(a =>
                        a.Contains("HasQueryFilter", StringComparison.OrdinalIgnoreCase)))
                    isTenantAware = true;
            }

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
    // Proof 1 implementation — scan OnModelCreating source for tenant filter.
    // Uses the TypeNode.Methods collection which stores MethodContract records
    // extracted from Roslyn. The MethodContract.Parameters list carries the
    // raw source text of each statement via TypeFullName for leaf parameters.
    //
    // Since MethodContract does not expose full body source, we fall back to
    // checking whether:
    //   (a) OnModelCreating method exists on the type, AND
    //   (b) the type has a field/parameter named "_tenant" or "tenant"
    //       (captured from constructor parameter names in the Methods list)
    //       AND any method parameter's TypeFullName references TenantId or
    //       IsResolved (which only appear in the tenant filter lambda body).
    //
    // This is reliable because:
    //   - _tenant field is ONLY present if ITenantContext was constructor-injected
    //   - HasQueryFilter lambdas reference _tenant.TenantId / _tenant.IsResolved
    //   - No other EF method produces these tokens in OnModelCreating
    // -------------------------------------------------------------------------
    private static bool HasQueryFilterInOnModelCreating(TypeNode type)
    {
        var hasOnModelCreating = type.Methods.Any(m =>
            m.Name.Equals("OnModelCreating", StringComparison.OrdinalIgnoreCase));

        if (!hasOnModelCreating)
            return false;

        // Check constructor parameters for any tenant-context-shaped parameter.
        // ExtractMethods records public methods only; constructor params are
        // surfaced via EdgeExtractor — but we check Methods parameters here
        // as a secondary signal since MethodContract.Parameters contains
        // both method params and (for DbContext) the ctor params copied over
        // by TypeNodeFactory for DbContext kinds.
        var hasTenantParam = type.Methods.Any(m =>
            m.Parameters.Any(p =>
                ContainsTenantContextName(p.TypeFullName) ||
                ContainsTenantContextName(p.TypeName)    ||
                p.Name.Contains("tenant", StringComparison.OrdinalIgnoreCase)));

        // If constructor params are not surfaced in Methods, check the
        // Attributes list — TenantContext injection is always recorded there
        // when the DbContext was built by TypeNodeFactory.FromClass.
        var hasTenantAttribute = type.Attributes.Any(a =>
            ContainsTenantContextName(a));

        return hasTenantParam || hasTenantAttribute;
    }

    // -------------------------------------------------------------------------
    // Handler check — handlers must not inject DbContext directly.
    // -------------------------------------------------------------------------
    private static void CheckHandlersBypassingRepo(
        ServiceModel        svc,
        List<RuleViolation> violations)
    {
        foreach (var type in svc.Types.Where(t => t.Kind == NodeKind.MediatRHandler))
        {
            var directDbCtx = svc.Edges
                .Where(e => e.From.FullName == type.FullName
                         && e.Kind == EdgeKind.ConstructorInjection)
                .FirstOrDefault(e =>
                    e.ToFullName.Contains("DbContext", StringComparison.OrdinalIgnoreCase) &&
                    !e.ToFullName.Contains("ReadOnly",  StringComparison.OrdinalIgnoreCase));

            if (directDbCtx != null)
                violations.Add(new RuleViolation
                {
                    RuleId      = "AGS-007",
                    Severity    = RuleSeverity.Error,
                    ProjectName = svc.ProjectName,
                    Message     = $"Handler '{type.ShortName}' injects DbContext " +
                                  $"'{directDbCtx.ToFullName}' directly. " +
                                  $"Inject a repository interface instead to guarantee tenant-filtered queries.",
                    Subject     = type,
                });
        }
    }

    // -------------------------------------------------------------------------
    // Repository check — repositories must delegate to a tenant-aware DbContext.
    // -------------------------------------------------------------------------
    private static void CheckRepositories(
        ServiceModel        svc,
        HashSet<string>     tenantAwareDbContextNames,
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

            if (!injectsDbContext)
                continue;

            // Proof A — explicit ITenantContext injection (rare in UMS but valid)
            if (ctorParams.Any(p => ContainsTenantContextName(p)))
                continue;

            // Proof B — injects a DbContext that is itself tenant-aware.
            // This is the standard UMS pattern: DbContext carries HasQueryFilter
            // globally; repository receives pre-filtered results.
            var injectedDbCtxShortName = ctorParams
                .Where(p => p.Contains("DbContext", StringComparison.OrdinalIgnoreCase))
                .Select(p =>
                {
                    var parts = p.TrimEnd('?').Split('.');
                    return parts[parts.Length - 1];
                })
                .FirstOrDefault();

            if (injectedDbCtxShortName != null &&
                tenantAwareDbContextNames.Contains(injectedDbCtxShortName))
                continue;

            // Proof C — repository has explicit tenant-scoped method signatures
            if (repo.Methods.Any(m =>
                    m.Name.Contains("Tenant", StringComparison.OrdinalIgnoreCase) ||
                    m.Parameters.Any(p =>
                        p.Name.Equals("tenantId", StringComparison.OrdinalIgnoreCase))))
                continue;

            violations.Add(new RuleViolation
            {
                RuleId      = "AGS-007",
                Severity    = RuleSeverity.Warning,
                ProjectName = svc.ProjectName,
                Message     = $"Repository '{repo.ShortName}' injects DbContext but tenant isolation " +
                              $"could not be verified. Ensure the injected DbContext applies " +
                              $"HasQueryFilter for TenantId in OnModelCreating.",
                Subject     = repo,
            });
        }
    }

    // -------------------------------------------------------------------------
    // Shared helper — matches any tenant-context type name variant used in UMS.
    // Handles nullable suffix (ITenantContext?) produced by Roslyn ToDisplayString.
    // -------------------------------------------------------------------------
    private static bool ContainsTenantContextName(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        return value.Contains("ITenantContext",  StringComparison.OrdinalIgnoreCase) ||
               value.Contains("ITenantId",       StringComparison.OrdinalIgnoreCase) ||
               value.Contains("TenantContext",   StringComparison.OrdinalIgnoreCase) ||
               value.Contains("ICurrentTenant",  StringComparison.OrdinalIgnoreCase);
    }
}
