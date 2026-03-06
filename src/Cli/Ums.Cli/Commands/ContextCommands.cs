namespace Ums.Cli.Commands;

using System.CommandLine;
using Spectre.Console;
using Ums.Cli.Infrastructure;

public static class ContextCommands
{
    private const string ContextDir = "/home/harsh/ums/context";
    private const string SnapDir    = "/home/harsh/ums/context/snapshots";

    public static Command Build()
    {
        var ctx = new Command("context", "Project context snapshot management");
        ctx.AddCommand(BuildDump());
        ctx.AddCommand(BuildShow());
        ctx.AddCommand(BuildHistory());
        return ctx;
    }

    private static Command BuildDump()
    {
        var cmd = new Command("dump", "Regenerate the live UMS_CONTEXT.md snapshot");
        cmd.SetHandler(() => BashBridge.Run("ums-dump.sh", ""));
        return cmd;
    }

    private static Command BuildShow()
    {
        var cmd = new Command("show", "Print the current context snapshot to stdout");
        cmd.SetHandler(() =>
        {
            var file = Path.Combine(ContextDir, "UMS_CONTEXT.md");
            if (!File.Exists(file))
            {
                AnsiConsole.MarkupLine("[yellow]  No context found. Run: ums context dump[/]");
                return Task.CompletedTask;
            }
            Console.WriteLine(File.ReadAllText(file));
            return Task.CompletedTask;
        });
        return cmd;
    }

    private static Command BuildHistory()
    {
        var cmd = new Command("history", "List all saved context snapshots");
        cmd.SetHandler(() =>
        {
            if (!Directory.Exists(SnapDir))
            {
                AnsiConsole.MarkupLine("[dim]  No snapshot history found.[/]");
                return Task.CompletedTask;
            }
            var table = new Table().BorderColor(Color.Grey);
            table.AddColumn("Timestamp");
            table.AddColumn("Size");
            table.AddColumn("Path");
            foreach (var f in Directory.GetFiles(SnapDir, "*.md")
                .OrderByDescending(File.GetLastWriteTimeUtc))
            {
                table.AddRow(
                    Path.GetFileNameWithoutExtension(f),
                    $"{new FileInfo(f).Length / 1024.0:F1} KB",
                    f);
            }
            AnsiConsole.Write(table);
            return Task.CompletedTask;
        });
        return cmd;
    }
}
