using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Spectre.Console;

namespace Ums.Cli.AI;

/// <summary>
/// Thin streaming wrapper around Anthropic /v1/messages.
/// Reads ANTHROPIC_API_KEY from the environment at construction time.
/// </summary>
public sealed class ClaudeClient : IDisposable
{
    const string Model  = "claude-sonnet-4-20250514";
    const string ApiUrl = "https://api.anthropic.com/v1/messages";

    readonly HttpClient _http;

    public ClaudeClient()
    {
        var key = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException(
                "ANTHROPIC_API_KEY is not set. " +
                "Run: export ANTHROPIC_API_KEY=sk-ant-...");

        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
        _http.DefaultRequestHeaders.Add("x-api-key", key);
        _http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        _http.DefaultRequestHeaders.Accept
             .Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
    }

    /// <summary>Yields text chunks as they arrive from the SSE stream.</summary>
    public async IAsyncEnumerable<string> StreamAsync(
        string system,
        string user,
        int maxTokens = 2048,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(new
        {
            model      = Model,
            max_tokens = maxTokens,
            stream     = true,
            system,
            messages   = new[] { new { role = "user", content = user } }
        });

        using var req = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        using var resp = await _http.SendAsync(
            req, HttpCompletionOption.ResponseHeadersRead, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Anthropic API {(int)resp.StatusCode}: {err}");
        }

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) yield break;
            if (string.IsNullOrWhiteSpace(line))  continue;
            if (!line.StartsWith("data: "))        continue;

            var json = line["data: ".Length..];
            if (json == "[DONE]") yield break;

            JsonNode? node;
            try   { node = JsonNode.Parse(json); }
            catch { continue; }

            if (node?["type"]?.GetValue<string>() != "content_block_delta") continue;

            var text = node["delta"]?["text"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(text)) yield return text;
        }
    }

    /// <summary>
    /// Streams response, calling <paramref name="onChunk"/> for each text piece.
    /// Returns the full assembled response.
    /// </summary>
    public async Task<string> AskAsync(
        string system,
        string user,
        Action<string>? onChunk = null,
        int maxTokens = 2048,
        CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        await foreach (var chunk in StreamAsync(system, user, maxTokens, ct))
        {
            sb.Append(chunk);
            onChunk?.Invoke(chunk);
        }
        return sb.ToString();
    }

    public void Dispose() => _http.Dispose();
}