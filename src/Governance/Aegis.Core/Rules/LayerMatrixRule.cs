namespace Aegis.Core.Rules;

using Aegis.Core.Model;
using System.Text.Json;

public sealed class LayerMatrix
{
    public Dictionary<string, List<string>> Allowed { get; init; } = [];

    public static LayerMatrix CleanArchitecture() => new()
    {
        Allowed = new()
        {
            ["API"]            = ["Application", "Domain", "SharedKernel"],
            ["Application"]    = ["Domain", "SharedKernel"],
            ["Infrastructure"] = ["Application", "Domain", "SharedKernel"],
            ["Domain"]         = ["SharedKernel"],
            ["SharedKernel"]   = [],
        }
    };

    public static async Task<LayerMatrix> LoadAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<LayerMatrix>(json) ?? CleanArchitecture();
    }
}

public static class PolicyLoader
{
    public static async Task<LayerMatrix> LoadMatrixAsync(string path) =>
        await LayerMatrix.LoadAsync(path);
}

/// <summary>
/// AGS-008 v1.1 — Layer Matrix Compliance
///
/// Enforces Clean Architecture dependency rules across all service layers.
///
/// v1.1 adds two production-correct exemptions that v1.0 falsely flagged:
///
///   Exemption 1 — Intra-Infrastructure: Repository/UnitOfWork → DbContext
///     Repositories and UnitOfWork implementations in Infrastructure are
///     explicitly allowed to reference their own service's DbContext.
///     This is the canonical Clean Architecture persistence pattern:
///       Application defines IRepository (interface) — Infrastructure implements it
///       using DbContext. The Infrastructure → Infrastructure edge here is internal
///       to the persistence sub-layer and is not a boundary violation.
///     Guard: "same service" is enforced by matching ProjectName on both ends,
///     preventing cross-service DbContext access which WOULD be a real violation.
///
///   Exemption 2 — EF Migration ModelSnapshot → ModelSnapshot (Microsoft)
///     EF Core tooling auto-generates *DbContextModelSnapshot.cs files that
///     inherit Microsoft.EntityFrameworkCore.Infrastructure.ModelSnapshot.
///     These are generated artefacts outside developer control and carry no
///     architectural meaning. Flagging them creates noise that buries real issues.
/// </summary>
public sealed class LayerMatrixRule : IRule
{
    private readonly LayerMatrix _matrix;

    public LayerMatrixRule(LayerMatrix matrix) => _matrix = matrix;

    public string       RuleId   => "AGS-008";
    public string       RuleName => "Layer Matrix Compliance";
    public RuleCategory Category => RuleCategory.Boundary;
    public string       Version  => "1.1";

    public RuleResult Evaluate(ArchitectureModel model)
    {
        var violations = new List<RuleViolation>();

        foreach (var svc in model.Services)
        foreach (var edge in svc.Edges)
        {
            if (edge.ToLayer == ArchitectureLayer.Unknown) continue;

            var fromLayer = edge.From.Layer.ToString();
            var toLayer   = edge.ToLayer.ToString();

            if (!_matrix.Allowed.TryGetValue(fromLayer, out var allowed)) continue;
            if (allowed.Contains(toLayer)) continue;

            // ------------------------------------------------------------------
            // Exemption 1 — Intra-Infrastructure persistence references.
            // A Repository or UnitOfWork referencing its own service's DbContext
            // is the correct Clean Architecture pattern. Only exempt when both
            // ends belong to the same project (same-service guard).
            // ------------------------------------------------------------------
            if (IsIntraInfrastructurePersistenceEdge(edge, svc.ProjectName))
                continue;

            // ------------------------------------------------------------------
            // Exemption 2 — EF Core auto-generated migration snapshot files.
            // *ModelSnapshot inherits Microsoft.EntityFrameworkCore.Infrastructure
            // .ModelSnapshot — generated code, zero architectural relevance.
            // ------------------------------------------------------------------
            if (IsEfMigrationSnapshot(edge))
                continue;

            violations.Add(new RuleViolation
            {
                RuleId      = RuleId,
                Severity    = RuleSeverity.Warning,
                ProjectName = svc.ProjectName,
                Message     = $"[{fromLayer}] '{edge.From.ShortName}' → [{toLayer}] '{edge.ToFullName}' " +
                              $"violates layer matrix policy.",
                Subject     = edge.From,
                Edge        = edge,
            });
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
    // Exemption 1 — Repository/UnitOfWork → DbContext within same service.
    // Both ends must be Infrastructure and belong to the same project.
    // -------------------------------------------------------------------------
    private static bool IsIntraInfrastructurePersistenceEdge(
        DependencyEdge edge, string serviceProjectName)
    {
        if (edge.From.Layer != ArchitectureLayer.Infrastructure) return false;
        if (edge.ToLayer    != ArchitectureLayer.Infrastructure) return false;

        // Same-service guard: target project must match or be unresolved (SharedKernel types
        // from external assemblies have null ToProjectName — those are handled by the
        // allowed-list; this exemption is only for intra-service Infrastructure edges).
        var sameService = edge.ToProjectName == null ||
                          edge.ToProjectName.Equals(serviceProjectName,
                              StringComparison.OrdinalIgnoreCase);
        if (!sameService) return false;

        var fromName = edge.From.ShortName;
        var toName   = edge.ToFullName;

        // Source must be a Repository or UnitOfWork
        var fromIsRepo = fromName.EndsWith("Repository",  StringComparison.OrdinalIgnoreCase) ||
                         fromName.EndsWith("UnitOfWork",  StringComparison.OrdinalIgnoreCase);

        // Target must be a DbContext (own service's context)
        var toIsDbContext = toName.EndsWith("DbContext",  StringComparison.OrdinalIgnoreCase) ||
                            toName.Contains("DbContext.", StringComparison.OrdinalIgnoreCase);

        return fromIsRepo && toIsDbContext;
    }

    // -------------------------------------------------------------------------
    // Exemption 2 — EF Core auto-generated ModelSnapshot artefacts.
    // Source name ends with "ModelSnapshot"; target is Microsoft EF namespace.
    // -------------------------------------------------------------------------
    private static bool IsEfMigrationSnapshot(DependencyEdge edge)
    {
        var fromIsSnapshot = edge.From.ShortName.EndsWith(
            "ModelSnapshot", StringComparison.OrdinalIgnoreCase);

        var toIsMicrosoftEf =
            edge.ToFullName.Contains("Microsoft.EntityFrameworkCore",
                StringComparison.OrdinalIgnoreCase) ||
            edge.ToFullName.Contains("ModelSnapshot",
                StringComparison.OrdinalIgnoreCase);

        return fromIsSnapshot && toIsMicrosoftEf;
    }
}
