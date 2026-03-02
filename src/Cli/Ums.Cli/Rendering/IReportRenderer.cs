namespace Ums.Cli.Rendering;

using Aegis.Core.Rules;

public interface IReportRenderer
{
    string Render(EngineReport report);
}