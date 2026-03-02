namespace Ums.Cli.Rendering;

public static class RendererFactory
{
    public static IReportRenderer Create(string format) => format.ToLower() switch
    {
        "json"    => new JsonReportRenderer(),
        "compact" => new CompactReportRenderer(),
        _         => new TextReportRenderer(),
    };
}