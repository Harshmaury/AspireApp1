namespace Ums.Cli;

using Aegis.Core.Rules;

internal static class ExitCodeResolver
{
    public static int Resolve(EngineReport report, RuleSeverity threshold, bool failOnWarning)
    {
        var violations = report.AllViolations
            .Where(v => v.Severity >= threshold)
            .ToList();

        if (violations.Any(v => v.Severity >= RuleSeverity.Error))   return 1;
        if (failOnWarning && violations.Any(v => v.Severity == RuleSeverity.Warning)) return 2;
        return 0;
    }

    public static void ApplyAndExit(EngineReport report, RuleSeverity threshold, bool failOnWarning, string cmd)
    {
        var code = Resolve(report, threshold, failOnWarning);
        switch (code)
        {
            case 1:
                Console.Error.WriteLine($"[UMS] FAIL {cmd} — {report.ErrorCount} error(s), {report.WarningCount} warning(s)");
                Environment.Exit(1); break;
            case 2:
                Console.Error.WriteLine($"[UMS] WARN {cmd} — {report.WarningCount} warning(s)");
                Environment.Exit(2); break;
            default:
                Console.WriteLine($"[UMS] PASS {cmd}"); break;
        }
    }
}