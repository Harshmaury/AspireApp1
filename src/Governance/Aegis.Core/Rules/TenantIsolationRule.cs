namespace Aegis.Core.Rules;
using Aegis.Core.Model;

public sealed class TenantIsolationRule : IRule
{
    public string       RuleId   => "AGS-007";
    public string       RuleName => "Tenant Isolation";
    public RuleCategory Category => RuleCategory.Tenant;
    public string       Version  => "1.1";

    public RuleResult Evaluate(ArchitectureModel model)
    {
        var violations = new List<RuleViolation>();

        foreach (var svc in model.Services)
        {
            CheckDbContexts(svc, violations);
            CheckHandlersBypassingRepo(svc, violations);
            CheckRepositoriesLackingTenantContext(svc, violations);
        }

        return new RuleResult { RuleId = RuleId, RuleName = RuleName, Category = Category, Version = Version, Violations = violations };
    }

    private static void CheckDbContexts(ServiceModel svc, List<RuleViolation> violations)
    {
        foreach (var type in svc.Types.Where(t => t.Kind == NodeKind.DbContext))
        {
            var hasTenantFilter =
                type.Methods.Any(m => m.Name.Equals("OnModelCreating",  StringComparison.OrdinalIgnoreCase)) ||
                type.Methods.Any(m => m.Name.Contains("Tenant",         StringComparison.OrdinalIgnoreCase) ||
                                      m.Name.Contains("Filter",         StringComparison.OrdinalIgnoreCase)) ||
                type.Attributes.Any(a => a.Contains("HasQueryFilter",   StringComparison.OrdinalIgnoreCase));

            if (!hasTenantFilter)
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

    // MediatR handlers injecting DbContext directly â€” bypass repository, bypass tenant filter
    private static void CheckHandlersBypassingRepo(ServiceModel svc, List<RuleViolation> violations)
    {
        foreach (var type in svc.Types.Where(t => t.Kind == NodeKind.MediatRHandler))
        {
            var directDbCtx = svc.Edges
                .Where(e => e.From.FullName == type.FullName && e.Kind == EdgeKind.ConstructorInjection)
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

    // Repositories must accept ITenantContext, ITenantId, or TenantId in constructor
    private static void CheckRepositoriesLackingTenantContext(ServiceModel svc, List<RuleViolation> violations)
    {
        var repoTypes = svc.Types.Where(t =>
            (t.ShortName.EndsWith("Repository", StringComparison.OrdinalIgnoreCase) ||
             t.Interfaces.Any(i => i.Contains("IRepository", StringComparison.OrdinalIgnoreCase))) &&
            t.Kind == NodeKind.Class);

        foreach (var repo in repoTypes)
        {
            var ctorParams = svc.Edges
                .Where(e => e.From.FullName == repo.FullName && e.Kind == EdgeKind.ConstructorInjection)
                .Select(e => e.ToFullName)
                .ToList();

            var hasTenantParam = ctorParams.Any(p =>
                p.Contains("ITenantContext",   StringComparison.OrdinalIgnoreCase) ||
                p.Contains("ITenantId",        StringComparison.OrdinalIgnoreCase) ||
                p.Contains("TenantContext",    StringComparison.OrdinalIgnoreCase) ||
                p.Contains("ICurrentTenant",   StringComparison.OrdinalIgnoreCase));

            // Only flag repos that DO inject a DbContext but NOT a tenant parameter
            var injectsDbContext = ctorParams.Any(p => p.Contains("DbContext"));
            if (injectsDbContext && !hasTenantParam)
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



