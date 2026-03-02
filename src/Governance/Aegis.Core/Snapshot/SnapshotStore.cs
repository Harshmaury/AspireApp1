namespace Aegis.Core.Snapshot;

using Aegis.Core.Model;
using System.Text.Json;

public static class SnapshotStore
{
    private static readonly JsonSerializerOptions _opts = new() { WriteIndented = true };

    public static async Task SaveAsync(string path, ArchitectureModel model)
    {
        var services = model.Services.Select(ServiceSnapshot.From).ToList();
        var snap = new PersistedSnapshot
        {
            FormatVersion = model.FormatVersion,
            CapturedAt    = model.CapturedAt,
            ContentHash   = ComputeHash(services),
            Services      = services,
        };
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(snap, _opts));
    }

    public static async Task<PersistedSnapshot> LoadAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<PersistedSnapshot>(json)
               ?? throw new InvalidOperationException($"Invalid snapshot: {path}");
    }

    public static void Prune(string dir, string prefix, int keepCount = 5)
    {
        Directory.GetFiles(dir, $"{prefix}-*.snap.json")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Skip(keepCount)
            .ToList()
            .ForEach(f => { File.Delete(f); Console.WriteLine($"[UMS] Pruned: {Path.GetFileName(f)}"); });
    }

    internal static string ComputeServiceHash(ServiceSnapshot svc)
    {
        var lines = svc.Types
            .Select(t => $"{t.FullName}|{t.Layer}|{t.Kind}|{string.Join(",", t.Methods.OrderBy(x => x))}|{string.Join(",", t.Interfaces.OrderBy(x => x))}|{string.Join(",", t.BaseTypes.OrderBy(x => x))}|{string.Join(",", t.Attributes.OrderBy(x => x))}|{string.Join(",", t.Endpoints.OrderBy(x => x))}")
            .Concat(svc.KafkaProducers.Select(k => $"KP:{svc.Name}:{k}"))
            .Concat(svc.KafkaConsumers.Select(k => $"KC:{svc.Name}:{k}"))
            .Concat(svc.DiRegistrations.Select(d => $"DI:{svc.Name}:{d}"))
            .OrderBy(x => x);

        using var sha  = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(string.Join("\n", lines)));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    internal static string ComputeHash(List<ServiceSnapshot> services)
    {
        var lines = services
            .OrderBy(s => s.Name)
            .SelectMany(s =>
                s.Types.Select(t => $"{t.FullName}|{t.Layer}|{t.Kind}|{string.Join(",", t.Methods.OrderBy(x => x))}|{string.Join(",", t.Interfaces.OrderBy(x => x))}|{string.Join(",", t.BaseTypes.OrderBy(x => x))}|{string.Join(",", t.Attributes.OrderBy(x => x))}|{string.Join(",", t.Endpoints.OrderBy(x => x))}")
                .Concat(s.KafkaProducers.Select(k => $"KP:{s.Name}:{k}"))
                .Concat(s.KafkaConsumers.Select(k => $"KC:{s.Name}:{k}"))
                .Concat(s.DiRegistrations.Select(d => $"DI:{s.Name}:{d}")))
            .OrderBy(x => x);

        using var sha  = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(string.Join("\n", lines)));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}