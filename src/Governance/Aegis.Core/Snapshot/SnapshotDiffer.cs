namespace Aegis.Core.Snapshot;

using Aegis.Core.Model;

public record DiffResult(bool HasDrift, List<string> Added, List<string> Removed, List<string> Changed)
{
    public IReadOnlyDictionary<string, ServiceDiffResult> ByService { get; init; } =
        new Dictionary<string, ServiceDiffResult>();
}

public record ServiceDiffResult(
    string ServiceName, bool HasDrift,
    List<string> Added, List<string> Removed, List<string> Changed);

public static class SnapshotDiffer
{
    public static DiffResult Diff(PersistedSnapshot baseline, ArchitectureModel current)
    {
        if (baseline.ContentHash != null)
        {
            var currentServices = current.Services.Select(ServiceSnapshot.From).ToList();
            if (SnapshotStore.ComputeHash(currentServices) == baseline.ContentHash)
                return new DiffResult(false, [], [], []);
        }

        var baselineMap  = baseline.Services.ToDictionary(s => s.Name, StringComparer.Ordinal);
        var currentMap   = current.Services.ToDictionary(s => s.ProjectName, StringComparer.Ordinal);
        var allNames     = baselineMap.Keys.Union(currentMap.Keys).ToList();
        var serviceResults = new Dictionary<string, ServiceDiffResult>();
        var allAdded     = new List<string>();
        var allRemoved   = new List<string>();
        var allChanged   = new List<string>();

        foreach (var name in allNames)
        {
            var hasBase    = baselineMap.TryGetValue(name, out var baseSvc);
            var hasCurrent = currentMap.TryGetValue(name, out var currSvc);

            if (!hasBase)
            {
                var added = currSvc!.Types.Select(t => t.FullName).ToList();
                serviceResults[name] = new ServiceDiffResult(name, true, added, [], []);
                allAdded.AddRange(added); continue;
            }
            if (!hasCurrent)
            {
                var removed = baseSvc!.Types.Select(t => t.FullName).ToList();
                serviceResults[name] = new ServiceDiffResult(name, true, [], removed, []);
                allRemoved.AddRange(removed); continue;
            }

            var currentSnap = ServiceSnapshot.From(currSvc!);
            var currentHash = SnapshotStore.ComputeServiceHash(currentSnap);
            if (baseSvc!.ServiceHash != null && baseSvc.ServiceHash == currentHash)
            {
                serviceResults[name] = new ServiceDiffResult(name, false, [], [], []);
                continue;
            }

            var baseSet    = Canonicalise(baseSvc);
            var currentSet = Canonicalise(currentSnap);
            var sAdded     = currentSet.Keys.Except(baseSet.Keys).Select(k => currentSet[k]).ToList();
            var sRemoved   = baseSet.Keys.Except(currentSet.Keys).Select(k => baseSet[k]).ToList();
            var sChanged   = currentSet.Keys.Intersect(baseSet.Keys)
                .Where(k => currentSet[k] != baseSet[k])
                .Select(k => $"{k} | was: {baseSet[k]} | now: {currentSet[k]}").ToList();

            serviceResults[name] = new ServiceDiffResult(name,
                sAdded.Count > 0 || sRemoved.Count > 0 || sChanged.Count > 0,
                sAdded, sRemoved, sChanged);
            allAdded.AddRange(sAdded); allRemoved.AddRange(sRemoved); allChanged.AddRange(sChanged);
        }

        return new DiffResult(
            allAdded.Count > 0 || allRemoved.Count > 0 || allChanged.Count > 0,
            allAdded, allRemoved, allChanged)
        { ByService = serviceResults };
    }

    private static Dictionary<string, string> Canonicalise(ServiceSnapshot svc)
    {
        var dict = svc.Types.ToDictionary(t => t.FullName, Fingerprint);
        dict[$"__kafka_producers__{svc.Name}"]  = string.Join(",", svc.KafkaProducers.OrderBy(x => x));
        dict[$"__kafka_consumers__{svc.Name}"]  = string.Join(",", svc.KafkaConsumers.OrderBy(x => x));
        dict[$"__di_registrations__{svc.Name}"] = string.Join(",", svc.DiRegistrations.OrderBy(x => x));
        return dict;
    }

    private static string Fingerprint(PersistedTypeSnapshot t) =>
        $"LAYER:{t.Layer}|KIND:{t.Kind}|" +
        $"METHODS:{string.Join(",", t.Methods.OrderBy(x => x))}|" +
        $"INTERFACES:{string.Join(",", t.Interfaces.OrderBy(x => x))}|" +
        $"BASETYPES:{string.Join(",", t.BaseTypes.OrderBy(x => x))}|" +
        $"ATTRIBUTES:{string.Join(",", t.Attributes.OrderBy(x => x))}|" +
        $"ENDPOINTS:{string.Join(",", t.Endpoints.OrderBy(x => x))}";
}