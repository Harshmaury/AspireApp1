namespace Aegis.Core.Building;

using Aegis.Core.Model;
using Microsoft.CodeAnalysis;

internal static class EdgeExtractor
{
    public static IEnumerable<DependencyEdge> FromConstructors(
        INamedTypeSymbol sym, TypeNode fromNode,
        SemanticModel model, string projectName, HashSet<string>? loadedProjectNames = null)
    {
        return sym.Constructors
            .SelectMany(c => c.Parameters)
            .Select(p => new DependencyEdge
            {
                From          = fromNode,
                ToFullName    = p.Type.ToDisplayString(),
                ToNamespace   = p.Type.ContainingNamespace.ToDisplayString(),
                Kind          = EdgeKind.ConstructorInjection,
                ToProjectName = ResolveProject(p.Type, loadedProjectNames),
                ToLayer       = ResolveLayer(p.Type),
            });
    }

    public static IEnumerable<DependencyEdge> FromInheritance(
        INamedTypeSymbol sym, TypeNode fromNode, string projectName, HashSet<string>? loadedProjectNames = null)
    {
        var edges = new List<DependencyEdge>();

        if (sym.BaseType != null && sym.BaseType.Name != "Object")
            edges.Add(new DependencyEdge
            {
                From          = fromNode,
                ToFullName    = sym.BaseType.ToDisplayString(),
                ToNamespace   = sym.BaseType.ContainingNamespace.ToDisplayString(),
                Kind          = EdgeKind.Inheritance,
                ToProjectName = ResolveProject(sym.BaseType, loadedProjectNames),
                ToLayer       = ResolveLayer(sym.BaseType),
            });

        edges.AddRange(sym.Interfaces.Select(i => new DependencyEdge
        {
            From          = fromNode,
            ToFullName    = i.ToDisplayString(),
            ToNamespace   = i.ContainingNamespace.ToDisplayString(),
            Kind          = EdgeKind.InterfaceImplementation,
            ToProjectName = ResolveProject(i, loadedProjectNames),
            ToLayer       = ResolveLayer(i),
        }));

        return edges;
    }

    internal static string? ResolveProject(ITypeSymbol type, HashSet<string>? loadedProjectNames)
    {
        var assemblyName = type.ContainingAssembly?.Name;
        if (assemblyName == null) return null;
        if (assemblyName.StartsWith("System") || assemblyName.StartsWith("Microsoft")) return null;
        if (loadedProjectNames != null && !loadedProjectNames.Contains(assemblyName)) return null;
        return assemblyName;
    }

    private static ArchitectureLayer ResolveLayer(ITypeSymbol type)
    {
        var assembly = type.ContainingAssembly?.Name ?? string.Empty;
        if (assembly.EndsWith(".Domain",         StringComparison.OrdinalIgnoreCase)) return ArchitectureLayer.Domain;
        if (assembly.EndsWith(".Application",    StringComparison.OrdinalIgnoreCase)) return ArchitectureLayer.Application;
        if (assembly.EndsWith(".Infrastructure", StringComparison.OrdinalIgnoreCase)) return ArchitectureLayer.Infrastructure;
        if (assembly.EndsWith(".SharedKernel",   StringComparison.OrdinalIgnoreCase)) return ArchitectureLayer.SharedKernel;
        if (assembly.EndsWith(".Contracts",      StringComparison.OrdinalIgnoreCase)) return ArchitectureLayer.SharedKernel;
        if (assembly.EndsWith(".API",            StringComparison.OrdinalIgnoreCase)) return ArchitectureLayer.API;

        var ns = type.ContainingNamespace.ToDisplayString();
        foreach (var segment in ns.Split('.'))
        {
            switch (segment)
            {
                case "Domain":         return ArchitectureLayer.Domain;
                case "Application":    return ArchitectureLayer.Application;
                case "Infrastructure": return ArchitectureLayer.Infrastructure;
                case "SharedKernel":
                case "Contracts":      return ArchitectureLayer.SharedKernel;
                case "API":
                case "Controllers":    return ArchitectureLayer.API;
            }
        }
        return ArchitectureLayer.Unknown;
    }
}