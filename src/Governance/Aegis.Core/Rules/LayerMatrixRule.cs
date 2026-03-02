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

public sealed class LayerMatrixRule : IRule
{
    private readonly LayerMatrix _matrix;

    public LayerMatrixRule(LayerMatrix matrix) => _matrix = matrix;

    public string       RuleId   => "AGS-008";
    public string       RuleName => "Layer Matrix Compliance";
    public RuleCategory Category => RuleCategory.Boundary;
    public string       Version  => "1.0";

    public RuleResult Evaluate(ArchitectureModel model)
    {
        var violations = new List<RuleViolation>();

        foreach (var svc in model.Services)
        foreach (var edge in svc.Edges)
        {
            var fromLayer = edge.From.Layer.ToString();
            var toLayer   = edge.ToLayer.ToString();

            if (edge.ToLayer == ArchitectureLayer.Unknown) continue;
            if (!_matrix.Allowed.TryGetValue(fromLayer, out var allowed)) continue;
            if (!allowed.Contains(toLayer))
            {
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
        }

        return new RuleResult { RuleId = RuleId, RuleName = RuleName, Category = Category, Version = Version, Violations = violations };
    }
}