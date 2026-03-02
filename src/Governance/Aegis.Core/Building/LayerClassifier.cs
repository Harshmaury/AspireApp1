namespace Aegis.Core.Building;

using Aegis.Core.Model;

public sealed class LayerClassifier
{
    private readonly Dictionary<string, ArchitectureLayer> _folderMap;

    public LayerClassifier(Dictionary<string, ArchitectureLayer> folderMap)
    {
        _folderMap = folderMap;
    }

    public static LayerClassifier Default() => new(new(StringComparer.OrdinalIgnoreCase)
    {
        ["Domain"]         = ArchitectureLayer.Domain,
        ["Application"]    = ArchitectureLayer.Application,
        ["Infrastructure"] = ArchitectureLayer.Infrastructure,
        ["Persistence"]    = ArchitectureLayer.Infrastructure,
        ["API"]            = ArchitectureLayer.API,
        ["Controllers"]    = ArchitectureLayer.API,
        ["SharedKernel"]   = ArchitectureLayer.SharedKernel,
        ["Contracts"]      = ArchitectureLayer.SharedKernel,
    });

    public ArchitectureLayer Classify(string filePath, string projectRoot)
    {
        var relative = Path.GetRelativePath(projectRoot, filePath);
        foreach (var segment in relative.Split(Path.DirectorySeparatorChar))
            if (_folderMap.TryGetValue(segment, out var layer)) return layer;
        return ArchitectureLayer.Unknown;
    }
}