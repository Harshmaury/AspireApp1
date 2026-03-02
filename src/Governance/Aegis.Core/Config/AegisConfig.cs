namespace Aegis.Core.Config;

using Aegis.Core.Rules;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class AegisConfig
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RuleSeverity FailLevel { get; init; } = RuleSeverity.Error;

    public IReadOnlyList<string> DisabledRules { get; init; } = [];
    public IReadOnlyList<string> ExcludedCategories { get; init; } = [];
    public IReadOnlyDictionary<string, string> SeverityOverrides { get; init; } =
        new Dictionary<string, string>();
    public IReadOnlyList<AllowedException> AllowedExceptions { get; init; } = [];
    public string? MinimumRuleVersion { get; init; }

    private static readonly JsonSerializerOptions _opts = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas         = true,
        ReadCommentHandling         = JsonCommentHandling.Skip,
    };

    public static AegisConfig Default() => new();

    public static async Task<AegisConfig> LoadAsync(string configPath)
    {
        if (!File.Exists(configPath)) return Default();
        var json = await File.ReadAllTextAsync(configPath);
        return JsonSerializer.Deserialize<AegisConfig>(json, _opts) ?? Default();
    }

    public static string DefaultConfigPath(string projectRoot) =>
        Path.Combine(projectRoot, "aegis.config.json");

    public void Apply(RuleEngineBuilder builder)
    {
        foreach (var id in DisabledRules)
            builder.Disable(id);

        foreach (var (id, sevStr) in SeverityOverrides)
            if (Enum.TryParse<RuleSeverity>(sevStr, ignoreCase: true, out var sev))
                builder.WithSeverityOverride(id, sev);

        var cats = ExcludedCategories
            .Select(c => Enum.TryParse<RuleCategory>(c, ignoreCase: true, out var cat)
                ? (RuleCategory?)cat : null)
            .Where(c => c.HasValue)
            .Select(c => c!.Value)
            .ToArray();
        if (cats.Length > 0)
            builder.ExcludeCategories(cats);

        if (MinimumRuleVersion != null)
            builder.WithMinimumVersion(MinimumRuleVersion);
    }

    public bool IsSuppressed(RuleViolation v) =>
        AllowedExceptions.Any(e =>
            e.RuleId == v.RuleId &&
            (e.Target == null ||
             e.Target == v.Subject?.FullName ||
             e.Target == v.ProjectName));
}

public sealed class AllowedException
{
    public required string RuleId { get; init; }
    public string? Target         { get; init; }
    public string? Reason         { get; init; }
}