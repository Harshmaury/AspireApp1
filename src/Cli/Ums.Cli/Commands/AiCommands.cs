using System.CommandLine;
using System.Text.RegularExpressions;
using Spectre.Console;
using Ums.Cli.AI;
using Ums.Cli.Infrastructure;

namespace Ums.Cli.Commands;

public static class AiCommands
{
    public static Command Build()
    {
        // ── ums ask ───────────────────────────────────────────────────────────
        var question   = new Argument<string>("question",
                             "Natural-language question about your cluster or codebase");
        var service    = new Option<string?>(["--service", "-s"],
                             "Focus pod logs on one service (identity, kafka, student …)");
        var noContext  = new Option<bool>("--no-context",
                             "Skip cluster context — answer from architecture knowledge only");
        var tokensAsk  = new Option<int>("--tokens", () => 2048,
                             "Max reply tokens");

        var ask = new Command("ask", "Ask UMS-AI a question with live cluster context")
        {
            question, service, noContext, tokensAsk
        };

        ask.SetHandler(async (q, svc, skipCtx, tok) =>
        {
            if (!CheckApiKey()) { Environment.Exit(1); return; }

            var context = "";
            if (!skipCtx)
            {
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("blue dim"))
                    .StartAsync("[blue]Collecting cluster context…[/]", async _ =>
                        context = await new ContextCollector().CollectAsync(svc));
            }

            var userMessage = string.IsNullOrEmpty(context)
                ? q
                : $"## Live Cluster State\n{context}\n\n## Question\n{q}";

            AnsiConsole.MarkupLine(
                "\n[bold cyan]── UMS-AI ──────────────────────────────────────────────[/]\n");

            using var cts    = new CancellationTokenSource();
            using var client = new ClaudeClient();
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

            try
            {
                await client.AskAsync(UmsSystemPrompt.Build(), userMessage,
                    onChunk: Console.Write, maxTokens: tok, ct: cts.Token);
            }
            catch (OperationCanceledException) { AnsiConsole.MarkupLine("\n[yellow]Interrupted.[/]"); }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"\n[red]Error:[/] {Markup.Escape(ex.Message)}");
                Environment.Exit(1);
            }

            AnsiConsole.MarkupLine(
                "\n\n[bold cyan]────────────────────────────────────────────────────────[/]\n");

        }, question, service, noContext, tokensAsk);

        // ── ums diagnose ──────────────────────────────────────────────────────
        var svcArg    = new Argument<string?>("service",
                            () => null,
                            "Service to diagnose — omit for full cluster") { Arity = ArgumentArity.ZeroOrOne };
        var remediate = new Option<bool>(["--remediate", "-r"],
                            "Offer to run each suggested fix command interactively");
        var tokensDx  = new Option<int>("--tokens", () => 3000, "Max reply tokens");

        var diagnose = new Command("diagnose",
            "AI-powered cluster/service diagnosis with optional interactive remediation")
        {
            svcArg, remediate, tokensDx
        };

        diagnose.SetHandler(async (svc, rem, tok) =>
        {
            if (!CheckApiKey()) { Environment.Exit(1); return; }

            var context = "";
            var target  = svc ?? "cluster";

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("yellow"))
                .StartAsync($"[yellow]Scanning {target}…[/]", async _ =>
                    context = await new ContextCollector().CollectAsync(svc));

            var task = svc is not null
                ? $"Diagnose the **{svc}** service. Identify every anomaly, explain " +
                  "the root cause, and provide exact remediation commands."
                : "Full-cluster health diagnosis. Identify all anomalies, rank by " +
                  "severity, and provide exact remediation commands for each.";

            var userMessage = $"""
                ## Live Cluster State
                {context}

                ## Diagnosis Task
                {task}

                Structure your response as:
                ### Health Summary
                Overall status: Healthy | Degraded | Critical

                ### Issues Found
                For each: **[SEVERITY]** title — description.

                ### Root Cause Analysis
                WHY each issue is happening.

                ### Remediation Plan
                Numbered steps with exact commands in ```bash blocks.
                Prefix any destructive command with ⚠️ WARNING.
                """;

            AnsiConsole.MarkupLine(
                $"\n[bold yellow]── UMS-AI Diagnosis: {target} ──────────────────────────────[/]\n");

            using var client = new ClaudeClient();
            var full = new System.Text.StringBuilder();

            try
            {
                await client.AskAsync(UmsSystemPrompt.Build(), userMessage,
                    onChunk: chunk => { full.Append(chunk); Console.Write(chunk); },
                    maxTokens: tok);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"\n[red]Error:[/] {Markup.Escape(ex.Message)}");
                Environment.Exit(1);
                return;
            }

            AnsiConsole.MarkupLine(
                "\n\n[bold yellow]────────────────────────────────────────────────────────[/]\n");

            if (!rem) return;

            // ── interactive remediation ───────────────────────────────────────
            var bashBlockRx = new Regex(@"```bash\s*\n(.*?)```",
                                  RegexOptions.Singleline);
            var commands = bashBlockRx.Matches(full.ToString())
                .Select(m => m.Groups[1].Value.Trim())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToList();

            if (commands.Count == 0)
            {
                AnsiConsole.MarkupLine("[dim]No bash commands found in the diagnosis.[/]");
                return;
            }

            AnsiConsole.MarkupLine(
                $"[yellow]Found [bold]{commands.Count}[/] remediation command(s).[/]\n");

            static bool IsDestructive(string cmd) =>
                cmd.Contains("delete")          ||
                cmd.Contains("rollout restart") ||
                cmd.Contains("scale 0")         ||
                cmd.Contains("reset");

            foreach (var (cmd, idx) in commands.Select((c, i) => (c, i + 1)))
            {
                AnsiConsole.MarkupLine($"[dim]{idx}.[/] [white]{Markup.Escape(cmd)}[/]");

                if (IsDestructive(cmd))
                    AnsiConsole.MarkupLine("  [red bold]⚠️  DESTRUCTIVE — review before running[/]");

                if (!AnsiConsole.Confirm("  Run this command?", defaultValue: !IsDestructive(cmd)))
                {
                    AnsiConsole.WriteLine();
                    continue;
                }

                AnsiConsole.MarkupLine("  [blue]Running…[/]");
                await BashBridge.RunAsync(cmd);
                AnsiConsole.WriteLine();
            }

        }, svcArg, remediate, tokensDx);

        // ── parent "ai" branch ────────────────────────────────────────────────
        var ai = new Command("ai", "UMS AI brain — ask questions, run diagnosis");
        ai.AddCommand(ask);
        ai.AddCommand(diagnose);

        // Also expose ask/diagnose at the root level for ergonomics:
        //   ums ask "..."       (top-level shortcut)
        //   ums diagnose svc    (top-level shortcut)
        // These are added in Program.cs directly alongside the "ai" branch.
        return ai;
    }

    // ── shared helpers ────────────────────────────────────────────────────────

    static bool CheckApiKey()
    {
        if (!string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")))
            return true;

        AnsiConsole.MarkupLine("[red bold]Error:[/] ANTHROPIC_API_KEY is not set.");
        AnsiConsole.MarkupLine("  [dim]export ANTHROPIC_API_KEY=sk-ant-...[/]");
        return false;
    }
}