namespace Ums.Cli.Rendering;

using Aegis.Core.Rules;
using System.Text;

public sealed class TextReportRenderer : IReportRenderer
{
    public string Render(EngineReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"AEGIS_FORMAT:2.0  Evaluated:{report.EvaluatedAt:u}");
        foreach (var r in report.Results)
        {
            sb.AppendLine($"[{(r.Passed ? "PASS" : "FAIL")}] [{r.RuleId} v{r.Version}] {r.RuleName}  ({r.Category})");
            foreach (var v in r.Violations)
            {
                var sev = v.Severity switch
                {
                    RuleSeverity.Error   => "ERR ",
                    RuleSeverity.Warning => "WARN",
                    _                   => "INFO",
                };
                sb.AppendLine($"     {sev}  {v.Message}");
                if (v.Subject != null)
                    sb.AppendLine($"          -> {v.Subject.FullName} [{v.Subject.Layer}]");
            }
        }
        sb.AppendLine($"SUMMARY  Errors:{report.ErrorCount}  Warnings:{report.WarningCount}  Status:{(report.Passed ? "PASS" : "FAIL")}");
        return sb.ToString();
    }
}