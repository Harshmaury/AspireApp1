namespace Ums.Cli.Rendering;

using Aegis.Core.Rules;
using System.Text.Json;

public sealed class JsonReportRenderer : IReportRenderer
{
    public string Render(EngineReport report)
    {
        var obj = new
        {
            format      = "AEGIS_FORMAT:2.0",
            evaluatedAt = report.EvaluatedAt,
            passed      = report.Passed,
            errorCount  = report.ErrorCount,
            warnCount   = report.WarningCount,
            rules       = report.Results.Select(r => new
            {
                id         = r.RuleId,
                name       = r.RuleName,
                category   = r.Category.ToString(),
                version    = r.Version,
                passed     = r.Passed,
                violations = r.Violations.Select(v => new
                {
                    severity = v.Severity.ToString(),
                    message  = v.Message,
                    project  = v.ProjectName,
                    subject  = v.Subject?.FullName,
                })
            })
        };
        return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
    }
}