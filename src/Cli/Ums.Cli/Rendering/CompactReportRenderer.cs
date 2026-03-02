namespace Ums.Cli.Rendering;

using Aegis.Core.Rules;

public sealed class CompactReportRenderer : IReportRenderer
{
    public string Render(EngineReport report) =>
        string.Join("\n", report.AllViolations.Select(v =>
            $"{v.Severity.ToString().ToUpper()}|{v.RuleId}|{v.ProjectName}|{v.Message}"));
}