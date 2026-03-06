namespace Ums.Cli.AI;

/// <summary>
/// Enforces a hard character ceiling on collected context.
/// ~4 chars ≈ 1 token → 24 000 chars ≈ 6 000 tokens, leaving 2 000 for the reply.
/// </summary>
public static class ContextWindow
{
    public const int MaxChars = 24_000;

    public static string Trim(string raw)
    {
        if (raw.Length <= MaxChars) return raw;

        // Keep the first 2/3 (cluster state) and last 1/3 (most recent logs).
        int keep  = MaxChars - 120;
        int front = keep * 2 / 3;
        int back  = keep - front;

        return raw[..front]
             + "\n\n[... middle of context trimmed to fit token budget ...]\n\n"
             + raw[^back..];
    }
}